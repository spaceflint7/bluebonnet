
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using Instruction = SpaceFlint.JavaBinary.JavaCode.Instruction;

namespace SpaceFlint.CilToJava
{

    public class CodeExceptions
    {

        //
        // exception handling
        //
        // the cil ExceptionHandler collection and the jvm Code attribute are
        // similar to the extent that a trivial translation might seem tempting,
        // except for some limitations:
        // - jvm has no support for clr filter handlers (introduced in C# 6).
        // - cil code can't catch java exceptions, e.g. an attempt to catch
        // System.DivideByZeroException will not work, because the jvm throws
        // java.lang.ArithmeticException.
        // - same as above when baselib wrappers call into java.  for example,
        // I/O calls might throw java.io.IOException while cil code catches
        // System.IO.IOException.
        //
        // to generalize the last two points:  the trivial approach does not
        // allow for a central point where we can map java exceptions to clr
        // counterparts.  therefore our conversion strategy is as follows.
        //
        // each 'try' block has one catch for java.lang.Throwable that stands
        // for all catch clauses.  (and a second catch for java.lang.Throwable
        // in case the 'try' block also includes a 'finally' clause.)
        //
        // the handler for that (first) java.lang.Throwable calls the method
        // system.Util.TranslateException(Throwable), which serves as the central
        // point for mapping exceptions.  parts of baselib can register mappings
        // such as java.io.IOException to System.IO.IOException.
        //
        // the handler then 'manually' compares the mapped exception type against
        // the clr catch clauses, and branches to the right clause, or otherwise,
        // rethrows the exception.  note that a filter clause is itself (in cil)
        // implemented as a catch for any exception type, and code that 'manually'
        // checks the type of the exception, so our own handler can implement the
        // filter clause by just branching to the start of the filter test code.
        //

        List<TryClause> tryClauses;
        Dictionary<int, CatchClause> catchClauses;
        CodeLocals locals;
        JavaCode code;
        JavaStackMap stackMap;

        internal class TryClause
        {
            internal int tryStart;
            internal int tryEnd;
            internal int catchEnd;
            internal int localIndex;
            internal List<CatchClause> catchClauses;
        }

        internal class CatchClause
        {
            internal TryClause tryClause;
            internal JavaType catchType;
            internal TypeReference catchFromType;
            internal int catchStart;
            internal int catchEnd;
            internal int filterCondStart;
            internal bool finallyClause;
            internal bool faultClause;
            internal bool includesRethrow;
            internal bool hasNestedTry;
            internal List<ushort> leaveOffsets;
        }



        public CodeExceptions(MethodBody body, JavaCode _code, CodeLocals locals)
        {
            tryClauses = new List<TryClause>();
            catchClauses = new Dictionary<int, CatchClause>();
            code = _code;
            stackMap = code.StackMap;
            this.locals = locals;

            if (body.HasExceptionHandlers)
            {
                if (! InitExceptionClauses(body))
                    throw CilMain.Where.Exception("error in exception handlers data");
            }
        }



        bool InitExceptionClauses(MethodBody body)
        {
            // scan cil exception records, where each record indicates the range
            // of code over which it is active (the TryStart .. TryEnd range).
            // based on this range, we group these catch clauses into 'try' blocks.

            foreach (var cilClause in body.ExceptionHandlers)
            {
                var tryClause = FindOrCreateTryClause(
                                    cilClause.TryStart.Offset, cilClause.TryEnd.Offset);
                if (tryClause == null)
                    return false;

                var catchClause = CreateCatchClause(tryClause, cilClause);
                if (catchClause == null)
                    return false;

                if (! catchClause.finallyClause)
                    ScanCatchClauseForRethrow(cilClause, catchClause);

                catchClause.tryClause = tryClause;
                tryClause.catchClauses.Add(catchClause);

                // in a filter clause, filterCondStart comes before catchStart
                int handlerOffset = catchClause.filterCondStart;
                if (handlerOffset == 0)
                    handlerOffset = catchClause.catchStart;

                if (catchClauses.ContainsKey(handlerOffset))
                    throw CilMain.Where.Exception("duplicate catch clause");
                catchClauses.Add(handlerOffset, catchClause);
            }

            // we now have a list of TryClauses, each representing a 'try' block,
            // with its related CatchClauses.  typically, a 'finally' clause has

            code.Exceptions = new List<JavaAttribute.Code.Exception>();
            JavaAttribute.Code.Exception exceptionItem;

            foreach (var tryClause in tryClauses)
            {
                // in a filter clause, filterCondStart comes before catchStart
                var firstClause = tryClause.catchClauses[0];
                int handlerOffset = firstClause.filterCondStart;
                if (handlerOffset == 0)
                {
                    handlerOffset = firstClause.catchStart;

                    ScanCatchClauseForNestedTry(firstClause);
                }

                exceptionItem.start = (ushort) tryClause.tryStart;
                exceptionItem.endPlus1 = (ushort) tryClause.tryEnd;
                exceptionItem.handler = (ushort) handlerOffset;
                exceptionItem.catchType = ThrowableType.ClassName;
                code.Exceptions.Add(exceptionItem);
            }

            return true;
        }



        TryClause FindOrCreateTryClause(int tryStart, int tryEnd)
        {
            // the CIL exception table lists exception handlers (which we call
            // CatchClauses), with the protected region for each handler.
            // we use a two-level hierarchy, with TryClauses listing protected
            // regions, and each TryClause listing the CatchClauses within it.
            // this function creates a new TryClause or reuses an existing one.

            if (tryStart >= tryEnd)
                return null;

            foreach (var oldTryClause in tryClauses)
            {
                if (oldTryClause.tryStart == tryStart && oldTryClause.tryEnd == tryEnd)
                    return oldTryClause;
            }

            var newTryClause = new TryClause();
            newTryClause.tryStart = tryStart;
            newTryClause.tryEnd = tryEnd;
            newTryClause.catchClauses = new List<CatchClause>();

            tryClauses.Add(newTryClause);
            return newTryClause;
        }



        static CatchClause CreateCatchClause(TryClause tryClause, ExceptionHandler cilClause)
        {
            // convert a CIL exception table entry to a CatchClause, while doing
            // some validity checks on the input.  in particular, we expect that
            // there are no gaps between catch clauses, and at most one finally
            // (or fault) clause within a protected region.

            var catchClause = new CatchClause();

            catchClause.catchStart = cilClause.HandlerStart.Offset;
            catchClause.catchEnd = cilClause.HandlerEnd?.Offset ?? 0xFFFF;
            int handlerStart = catchClause.catchStart;

            if (    catchClause.catchStart >= catchClause.catchEnd
                 || catchClause.catchStart < tryClause.tryEnd)
            {
                return null;
            }

            if (cilClause.HandlerType == ExceptionHandlerType.Catch)
            {
                // note that we treat 'catch (System.Exception)'
                // as 'catch (Throwable)' so that our try/catch blocks
                // can really see all exceptions

                var myType = CilType.From(cilClause.CatchType);
                if (myType.JavaName == "system.Exception" && (! myType.Equals(ThrowableType)))
                {
                    throw CilMain.Where.Exception(
                                "catch of system.Exception (use System.Exception)");
                }
                if (myType.Equals(JavaType.ObjectType))
                    catchClause.catchType = ThrowableType;
                else
                    catchClause.catchType = myType;
                if (myType.HasGenericParameters)
                    catchClause.catchFromType = cilClause.CatchType;
            }

            else if (cilClause.HandlerType == ExceptionHandlerType.Filter)
            {
                catchClause.catchType = ThrowableType;
                catchClause.filterCondStart = cilClause.FilterStart.Offset;
                handlerStart = catchClause.filterCondStart;

                if (catchClause.filterCondStart < tryClause.tryEnd)
                    return null;
            }

            else if (    cilClause.HandlerType == ExceptionHandlerType.Finally
                      || cilClause.HandlerType == ExceptionHandlerType.Fault)
            {
                catchClause.catchType = ThrowableType;
                catchClause.finallyClause = true;
                // note that we treat a 'fault' clause as a special form of a
                // 'finally' clause, so only one of these is allowed per 'try'.
                catchClause.faultClause =
                            (cilClause.HandlerType == ExceptionHandlerType.Fault);
            }
            else
                return null;

            bool consecutiveClauses = true;
            if (tryClause.catchClauses.Count == 0)
            {
                consecutiveClauses = (handlerStart == tryClause.tryEnd);
            }
            else
            {
                foreach (var oldCatchClause in tryClause.catchClauses)
                {
                    if (handlerStart <= oldCatchClause.catchStart)
                        consecutiveClauses = false;
                    if (oldCatchClause.finallyClause)
                        consecutiveClauses = false;
                }
            }

            if (! consecutiveClauses)
                throw CilMain.Where.Exception("non-consecutive clauses in a try block");

            if (catchClause.catchEnd > tryClause.catchEnd)
                tryClause.catchEnd = catchClause.catchEnd;

            return catchClause;
        }



        static void ScanCatchClauseForRethrow(ExceptionHandler clause, CatchClause catchClause)
        {
            // check if the catch clause contains a 'rethrow' instruction.
            // in such a case, we will need to save a copy of the exception.
            // see also ExceptionClauseSetup and SaveExceptionObject

            if (! catchClause.finallyClause)
            {
                var inst = clause.HandlerStart;
                while (inst != null && inst != clause.HandlerEnd)
                {
                    if (inst.OpCode.Code == Code.Rethrow)
                    {
                        catchClause.includesRethrow = true;
                        return;
                    }
                    inst = inst.Next;
                }
            }
        }



        void ScanCatchClauseForNestedTry(CatchClause catchClause)
        {
            // the stack contains an exception object on entry to all handlers,
            // regardless of the clause type.  a 'catch' or 'filter' handler
            // typically starts by storing (or popping) the object.
            // a 'finally' or 'fault' handler typically keeps the object on the
            // stack, for use by a later 'endfinally' instruction.
            //
            // however, if the 'finally' clause has a nested try/catch region,
            // then a thrown exception would clear the stack.  to work around
            // this, in such a case, we store the exception object in a local
            // (see also ExceptionClauseSetup and SaveExceptionObject), and
            // then load it in Translate_Endfinally

            if (catchClause.finallyClause)
            {
                foreach (var tryClause in tryClauses)
                {
                    if (    tryClause.tryStart >= catchClause.catchStart
                         && tryClause.tryEnd <= catchClause.catchEnd)
                    {
                        catchClause.hasNestedTry = true;
                    }
                }
            }
        }



        public void CheckAndSaveFrame(Mono.Cecil.Cil.Instruction inst)
        {
            int offset = inst.Offset;

            if (! catchClauses.TryGetValue(offset, out var catchClause))
            {
                //
                // on every instruction, and in particular on entry to a 'try'
                // block, we want to save the stack frame.
                //
                // - for the 'try' case:  due to possibility of any exception
                // occuring at any point within the try block, exception clauses
                // always roll back to that stack frame at the top of the 'try'.
                //
                // - for any other instruction:  because it may be the target
                // of a backwards jump at the end of a loop.
                //

                stackMap.SaveFrame((ushort) offset, false, CilMain.Where);
            }
            else
            {
                //
                // otherwise this is entry to a catch/filter/finally clause.
                //
                // if this is the first exception in the chain, we would also
                // need to push the exception type into the stack frame,
                // before saving it.
                //

                ExceptionClauseSetup((ushort) offset, catchClause);
            }
        }



        void ExceptionClauseSetup(ushort instOffset, CatchClause catchClause)
        {
            // if this is the first exception for the try block:
            //
            // the offset is recorded in the exception attribute, so we need
            // to generate a stack frame for it, which matches the stack frame
            // on entry to the try block, with a pushed java.lang.Throwable.

            var tryClause = catchClause.tryClause;

            if (catchClause == tryClause.catchClauses[0])
            {
                stackMap.LoadFrame((ushort) tryClause.tryStart, false, CilMain.Where);
                stackMap.PushStack(ThrowableType);
                stackMap.SaveFrame((ushort) instOffset, true, CilMain.Where);

                if (! catchClause.finallyClause)
                {
                    code.NewInstruction(0xB8 /* invokestatic */, CilType.SystemUtilType,
                                        new JavaMethodRef("TranslateException",
                                                          ThrowableType, ThrowableType));
                }
            }

            // if this is a finally clause, we have nothing further to do.
            // but if this is a filter clause, do some additional set up.

            if (catchClause.finallyClause)
            {
                if (catchClause.hasNestedTry)
                {
                    // see also:  ScanCatchClauseForNestedTry
                    SaveExceptionObject(tryClause, false);
                }

                if (catchClause.faultClause)
                {
                    // the 'fault' clause should start with a check whether
                    // an exception was thrown, which is very similar to what
                    // 'endfinally' does at the end of a normal 'finally' block,
                    // so we can reuse the same code.
                    Translate_Endfinally(instOffset, true);
                }

                return;
            }

            //
            // if this is a filter clause, we just need to initialize loals
            //

            if (catchClause.filterCondStart != 0)
            {
                // should the filter test pass, we need push the exception
                // object.  we don't know which local the filter test uses
                // to store the exception, so we make our own copy.  this
                // will be loaded by the 'endfilter' instruction, see there.

                SaveExceptionObject(tryClause);

                return;
            }

            //
            // for a non-filter catch clause that catches any kind of
            // exception, we don't need to test the exception type at all
            //

            if (catchClause.catchType.Equals(ThrowableType))
            {
                if (catchClause.includesRethrow)
                    SaveExceptionObject(tryClause);
            }

            //
            // otherwise, we do need to test the exception type, and
            // possibly branch to a secondary catch clause in the chain.
            //

            if (catchClause.catchFromType == null)
            {
                // exception type is plain type.  we will use follow up
                // instructions 'ifeq == zero' and 'ifne != zero',
                // see ExceptionClauseCommon

                if (catchClause.catchType.Equals(ThrowableType))
                {
                    code.NewInstruction(0x04 /* iconst_1 */, null, null);
                    stackMap.PushStack(JavaType.IntegerType);
                }
                else
                {
                    code.NewInstruction(0x59 /* dup */, null, null);
                    stackMap.PushStack(ThrowableType);
                    code.NewInstruction(0xC1 /* instanceof */, catchClause.catchType, null);
                }
            }
            else
            {
                // exception type is a generic type.  we will use follow up
                // instructions 'ifnull' and 'ifnonnnull'.
                // see also below in ExceptionClauseCommon
                code.NewInstruction(0x59 /* dup */, null, null);
                stackMap.PushStack(ThrowableType);
                GenericUtil.CastToGenericType(catchClause.catchFromType, 0, code);
            }

            stackMap.PopStack(CilMain.Where);

            ExceptionClauseCommon(catchClause);
        }



        void ExceptionClauseCommon(CatchClause catchClause)
        {
            // this code is generated at the top of a typical catch clause,
            // following the 'instanceof' generated by ExceptionClauseSetup.
            // for a filter clause, this is generated at the point of the
            // 'endfilter' instruction.
            //
            // in both cases, the stack contains [ Throwable, integer ],
            // where the integer is non-zero if the exception should be
            // handled, or zero if should be forwarded to the next clause.

            var nextClause = FindNextCatchClause(catchClause);

            if (nextClause != null)
            {
                int op = (catchClause.catchFromType == null)
                       ? 0x99 // ifeq == zero, for plain exception type
                       : 0xC6 // ifnull,       for generic exception type
                       ;      // see also ExceptionClauseSetup

                var nextClauseOffset = nextClause.filterCondStart;
                if (nextClauseOffset == 0)
                    nextClauseOffset = nextClause.catchStart;

                code.NewInstruction((byte) op, null, (ushort) nextClauseOffset);
                stackMap.SaveFrame((ushort) nextClauseOffset, true, CilMain.Where);
            }
            else
            {
                int op = (catchClause.catchFromType == null)
                       ? 0x9A // ifne != zero, for plain exception type
                       : 0xC7 // ifnonnull,    for generic exception type
                       ;      // see also ExceptionClauseSetup
                ushort label = locals.GetTempLabel();
                code.NewInstruction((byte) op, null, label);
                stackMap.SaveFrame(label, true, CilMain.Where);
                code.NewInstruction(0xBF /* athrow */, null, null);
                code.NewInstruction(0x00 /* nop */, null, null, label);
            }

            if (catchClause.includesRethrow)
                SaveExceptionObject(catchClause.tryClause);

            if (! catchClause.catchType.Equals(ThrowableType))
                code.NewInstruction(0xC0 /* checkcast */, catchClause.catchType, null);
        }



        void SaveExceptionObject(TryClause tryClause, bool keepOnStack = true)
        {
            if (keepOnStack)
            {
                stackMap.PushStack(ThrowableType);
                code.NewInstruction(0x59 /* dup */, null, null);
            }
            tryClause.localIndex = locals.GetTempIndex(ThrowableType);
            code.NewInstruction(0x3A /* astore */, null, tryClause.localIndex);
            stackMap.PopStack(CilMain.Where);
        }



        static bool IsNearest(int instOffset, int oldStart, int oldEnd, int newStart, int newEnd)
            => (    instOffset - newStart    <=   instOffset - oldStart
                 && newEnd     - instOffset  <=   oldEnd     - instOffset);



        TryClause FindNearestTryClause(int instOffset)
        {
            TryClause nearestClause = null;
            foreach (var tryClause in tryClauses)
            {
                if (instOffset >= tryClause.tryStart && instOffset < tryClause.tryEnd)
                {
                    if (    nearestClause == null
                         || IsNearest(instOffset,
                                      nearestClause.tryStart, nearestClause.tryEnd,
                                      tryClause.tryStart, tryClause.tryEnd))
                    {
                        nearestClause = tryClause;
                    }
                }
            }
            return nearestClause;
        }



        CatchClause FindNearestCatchClause(int instOffset)
        {
            // find the catch clause that contains offset of the current
            // instruction.  if a catch clause itself contains a 'try' block,
            // then multiple catch clauses may apply.  in this case we select
            // the catch clauses nerarest to the instruction, as that would
            // be the innermost catch clause.

            CatchClause nearestClause = null;
            foreach (var tryClause in tryClauses)
            {
                foreach (var catchClause in tryClause.catchClauses)
                {
                    if (instOffset >= catchClause.catchStart && instOffset < catchClause.catchEnd)
                    {
                        if (    nearestClause == null
                             || IsNearest(instOffset,
                                          nearestClause.catchStart, nearestClause.catchEnd,
                                          catchClause.catchStart, catchClause.catchEnd))
                        {
                            nearestClause = catchClause;
                        }
                    }
                }
            }
            return nearestClause;
        }



        CatchClause FindNearestFinallyClause(int instOffset)
        {
            CatchClause nearestClause = null;
            foreach (var tryClause in tryClauses)
            {
                if (instOffset >= tryClause.tryStart && instOffset < tryClause.tryEnd)
                {
                    if (tryClause.catchClauses.Count == 1)
                    {
                        var catchClause = tryClause.catchClauses[0];
                        if (catchClause.finallyClause)
                        {
                            if (    nearestClause == null
                                 || IsNearest(instOffset,
                                               nearestClause.catchStart, nearestClause.catchEnd,
                                               catchClause.catchStart, catchClause.catchEnd))
                            {
                                nearestClause = catchClause;
                            }
                        }
                    }
                }
            }
            return nearestClause;
        }



        CatchClause FindNextCatchClause(CatchClause catchClause)
        {
            var tryClause = catchClause.tryClause;

            // try to find the next catch clause within the same try block
            var allClauses = tryClause.catchClauses;
            int idx = allClauses.IndexOf(catchClause) + 1;
            if (idx > 0 && idx < allClauses.Count)
                return allClauses[idx];

            // try to find the corresponding finally handler
            return FindNearestFinallyClause(catchClause.catchEnd);
        }



        CatchClause RollbackStackFrame(int instOffset)
        {
            TryClause tryClause = FindNearestTryClause(instOffset);

            var clauseClause = FindNearestCatchClause(instOffset);
            if (clauseClause != null)
            {
                if (    tryClause == null
                     || IsNearest(instOffset,
                                  tryClause.tryStart, tryClause.tryEnd,
                                  clauseClause.catchStart, clauseClause.catchEnd))
                {
                    tryClause = clauseClause.tryClause;
                }
                else
                    clauseClause = null;
            }

            if (tryClause == null)
                throw new InvalidProgramException();

            if (! stackMap.LoadFrame((ushort) tryClause.tryStart, true, CilMain.Where))
                throw new InvalidProgramException();

            if (stackMap.StackArray().Length != 0)
                throw new InvalidProgramException();

            return clauseClause;
        }



        public void Translate(Mono.Cecil.Cil.Instruction inst)
        {
            switch (inst.OpCode.Code)
            {
                case Code.Throw:    case Code.Rethrow:  Translate_Throw(inst);      break;
                case Code.Leave:    case Code.Leave_S:  Translate_Leave(inst);      break;
                case Code.Endfinally:            Translate_Endfinally(inst.Offset); break;
                case Code.Endfilter:                    Translate_Endfilter(inst);  return;
                default:                                throw new InvalidProgramException();
            }

            //
            // these instructions break the normal flow of execution and clear the
            // stack frame.  if the following instruction already has a stack frame,
            // due to some earlier forward jump, we load that frame
            //

            CilMain.LoadFrameOrClearStack(stackMap, inst);

            locals.TrackUnconditionalBranch(inst);
        }



        void Translate_Throw(Mono.Cecil.Cil.Instruction inst)
        {
            if (inst.OpCode.Code == Code.Rethrow)
            {
                // if a catch clause contains a 'rethrow' instruction then
                // a reference to the exception object should have previously
                // been saved in a local (in SaveExceptionObject)

                CatchClause nearestClause = FindNearestCatchClause(inst.Offset);
                if (! nearestClause.includesRethrow)
                    throw new InvalidProgramException();
                code.NewInstruction(0x19 /* aload */, null,
                                    nearestClause.tryClause.localIndex);
            }

            // in the jvm, the stack trace is recorded when the exception
            // object is created.  in the clr, the stack trace is recorded
            // only when the exception is thrown, so we have to emulate it.
            //
            // (also note that our system.Exception replacement (in baselib)
            // discards the initial call to fillInStackTrace, which occurs
            // during the construction of the exception object.)

            code.NewInstruction(0xB6 /* invokevirtual */, ThrowableType,
                                new JavaMethodRef("fillInStackTrace", ThrowableType));
            code.NewInstruction(0xBF /* athrow */, null, null);
        }



        void Translate_Leave(Mono.Cecil.Cil.Instruction inst)
        {
            var catchClause = RollbackStackFrame(inst.Offset);

            if (catchClause != null && catchClause.finallyClause)
            {
                // leave is only permitted in a try or catch clauses,
                // but not a finally or a fault clause
                throw new InvalidProgramException();
            }

            if (inst.Operand is Mono.Cecil.Cil.Instruction leaveTarget)
            {
                bool directJump;
                var finallyClause1 = FindNearestFinallyClause(inst.Offset);

                if (finallyClause1 == null)
                {
                    // if there is no finally clause, we can jump directly
                    directJump = true;
                }
                else
                {
                    // check for a control transfer within the same try clause,
                    // in this case we do not need to handle the call to 'finally'
                    var finallyClause2 = FindNearestFinallyClause(leaveTarget.Offset);
                    directJump = (finallyClause1 == finallyClause2);
                }

                if (directJump)
                {
                    if (leaveTarget != inst.Next)
                    {
                        code.NewInstruction(0xA7 /* goto */, null, (ushort) leaveTarget.Offset);
                        stackMap.SaveFrame((ushort) leaveTarget.Offset, true, CilMain.Where);
                    }
                    else
                        code.NewInstruction(0x00 /* nop */, null, null);
                }
                else
                {
                    // if we have a 'finally' clause, the 'leave' target indicates
                    // where execution should resume, after executing the 'finally'
                    // handler. but the 'finally' handler does not specify where to
                    // resume.
                    //
                    // typically, normal execution resumes just past the end of the
                    // 'finally' handler, but there are exceptions to this rule;
                    // for example, the 'finally' handler may be immediately followed
                    // by a 'catch' clause from an enclosing try/catch block.
                    //
                    // in addition, the 'leave' target may specify some other offset,
                    // for example if the 'catch' contains a 'return' statement.
                    // and the try/catch block may not have any 'leave' instructions
                    // at all, if for example, all clauses end with a 'throw'.
                    //
                    // to work around all these issues, we create a 'fake' exception
                    // object that specifies the leave target.  this is identified
                    // in 'endfinally' and used to jump to the requested offset.
                    // see Translate_Endfinally.

                    ushort leaveOffset = (ushort) leaveTarget.Offset;
                    foreach (var tryClause in tryClauses)
                    {
                        var catchClauses = tryClause.catchClauses;
                        var lastClause = catchClauses[catchClauses.Count - 1];
                        if (lastClause.finallyClause)
                        {
                            if (lastClause.leaveOffsets == null)
                                lastClause.leaveOffsets = new List<ushort>();

                            if (lastClause.leaveOffsets.IndexOf(leaveOffset) == -1)
                                lastClause.leaveOffsets.Add(leaveOffset);
                        }
                    }

                    code.NewInstruction(0x12 /* ldc */, null, (int) leaveTarget.Offset);
                    stackMap.PushStack(JavaType.IntegerType);
                    code.NewInstruction(0xB8 /* invokestatic */, LeaveTargetType,
                                new JavaMethodRef("New", ThrowableType, JavaType.IntegerType));
                    stackMap.PopStack(CilMain.Where);

                    stackMap.PushStack(ThrowableType);

                    var finallyOffset = (ushort) finallyClause1.catchStart;
                    if (finallyOffset != inst.Next?.Offset)
                    {
                        code.NewInstruction(0xA7 /* goto */, null, (ushort) finallyOffset);
                        stackMap.SaveFrame((ushort) finallyOffset, true, CilMain.Where);
                    }

                    stackMap.PopStack(CilMain.Where); // pop null
                }
            }
            else
                throw new InvalidProgramException();
        }



        void Translate_Endfinally(int instOffset, bool setupFaultClause = false)
        {
            var thisClause = RollbackStackFrame(instOffset);
            if (thisClause == null || (! thisClause.finallyClause))
                throw new InvalidProgramException();

            int localIndex = -1;
            if (thisClause.hasNestedTry)
            {
                localIndex = thisClause.tryClause.localIndex;
                stackMap.SetLocal(localIndex, ThrowableType);
            }

            if (thisClause.faultClause && (! setupFaultClause))
            {
                // if this instruction appears in a 'fault' clause,
                // then it should be interpreted as 'endfault' rather
                // than 'endfilter', and just rethrow the exception
                if (thisClause.hasNestedTry)
                    code.NewInstruction(0x19 /* aload */, null, localIndex);
                code.NewInstruction(0xBF /* athrow */, null, null);
                return;
            }

            if (thisClause.catchEnd ==
                            FindNearestFinallyClause(instOffset)?.catchStart)
            {
                // if this 'finally' is itself nested within a 'finally'
                // handler that immediately follows it, just fall through
                stackMap.PushStack(ThrowableType);
                code.NewInstruction(0x00 /* nop */, null, null);
                stackMap.SaveFrame((ushort) instOffset, true, CilMain.Where);
                return;
            }

            // typical variant of a non-nested 'endfinally' instruction.
            // proceed to the specified leave offset, or rethrow the exception.

            if (! thisClause.hasNestedTry)
            {
                localIndex = locals.GetTempIndex(ThrowableType);
                stackMap.SetLocal(localIndex, JavaStackMap.Top);

                stackMap.PushStack(ThrowableType);
                code.NewInstruction(0x3A /* astore */, null, localIndex);
                stackMap.PopStack(CilMain.Where);
            }

            // if a try/catch exits due to non-exceptional control flow, then
            // the exception object will be a 'fake' exception object created
            // in Translate_Leave, see there.  we have to identify this, and
            // branch accordingly.

            if (thisClause.leaveOffsets != null)
            {
                code.NewInstruction(0x19 /* aload */, null, localIndex);
                code.NewInstruction(0xB8 /* invokestatic */, LeaveTargetType,
                            new JavaMethodRef("Get", JavaType.IntegerType, ThrowableType));

                int leaveIndex = -1;
                if (thisClause.leaveOffsets.Count != 1)
                {
                    leaveIndex = locals.GetTempIndex(JavaType.IntegerType);
                    code.NewInstruction(0x36 /* istore */, null, leaveIndex);
                }

                foreach (var leaveOffset in thisClause.leaveOffsets)
                {
                    if (leaveIndex != -1)
                        code.NewInstruction(0x15 /* iload */, null, leaveIndex);
                    stackMap.PushStack(JavaType.IntegerType);
                    code.NewInstruction(0x12 /* ldc */, null, (int) leaveOffset);
                    stackMap.PushStack(JavaType.IntegerType);
                    code.NewInstruction(0x9F /* if_icmpeq */, null, leaveOffset);
                    stackMap.PopStack(CilMain.Where);
                    stackMap.PopStack(CilMain.Where);
                    stackMap.SaveFrame(leaveOffset, true, CilMain.Where);
                }

                if (leaveIndex != -1)
                    locals.FreeTempIndex(localIndex);
            }

            // if the try/catch did exit due to an exception cause, then we
            // just need to rethrow the exception.

            code.NewInstruction(0x19 /* aload */, null, localIndex);
            if (setupFaultClause)
            {
                // if called from ExceptionClauseSetup to setup a 'fault' clause,
                // then we just want to put the exception back on the stack
                stackMap.PushStack(ThrowableType);
            }
            else
            {
                // if called to end a 'finally' clause, then we do want to throw
                code.NewInstruction(0xBF /* athrow */, null, null);
            }

            if (! thisClause.hasNestedTry)
            {
                // if this 'endfinally' instruction is the last in the clause,
                // then we can release the local index that holds the exception

                if (instOffset + 1 == thisClause.catchEnd)
                    locals.FreeTempIndex(localIndex);
            }
        }



        void Translate_Endfilter(Mono.Cecil.Cil.Instruction inst)
        {
            // operand stack should contain an integer
            var integer = stackMap.PopStack(CilMain.Where);
            if (! integer.Equals(JavaType.IntegerType))
                throw new InvalidProgramException();

            CatchClause filterClause = null;
            foreach (var tryClause in tryClauses)
            {
                foreach (var catchClause in tryClause.catchClauses)
                {
                    if (    catchClause.filterCondStart != 0
                         && catchClause.filterCondStart < inst.Offset
                         && catchClause.catchStart == inst.Next?.Offset)
                    {
                        filterClause = catchClause;
                        break;
                    }
                }
            }

            if (filterClause == null)
                throw new InvalidProgramException();

            // make the stack contain [ Throwable, integer ].  the integer
            // is already on the stack as the result of the filter test code.
            // the Throwable was stored in a local in ExceptionClauseSetup.

            var localIndex = filterClause.tryClause.localIndex;

            code.NewInstruction(0x19 /* aload */, null, localIndex);
            locals.FreeTempIndex(localIndex);

            stackMap.PushStack(ThrowableType);
            code.NewInstruction(0x5F /* swap */, null, null);

            ExceptionClauseCommon(filterClause);
        }



        internal static readonly JavaType ThrowableType =
                                            CilType.From(new JavaType(0, 0, "java.lang.Throwable"));
        internal static readonly JavaType LeaveTargetType =
                                                    new JavaType(0, 0, "system.TryCatchLeaveTarget");
    }
}
