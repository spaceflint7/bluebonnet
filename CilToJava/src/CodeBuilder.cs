
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using Instruction = SpaceFlint.JavaBinary.JavaCode.Instruction;

namespace SpaceFlint.CilToJava
{

    public partial class CodeBuilder
    {

        internal MethodDefinition defMethod;
        internal MethodBody defMethodBody;
        internal JavaMethod newMethod;

        internal CilMethod method;
        internal JavaCode code;

        internal Mono.Cecil.Cil.Instruction cilInst;
        internal Mono.Cecil.Cil.Code cilOp;

        internal CodeLocals locals;
        internal CodeArrays arrays;
        internal CodeExceptions exceptions;
        internal JavaStackMap stackMap;

        internal int lineNumber;



        internal static void BuildJavaCode(JavaMethod newMethod, CilMethod myMethod,
                                           MethodDefinition defMethod, int numCastableInterfaces)
        {
            var body = defMethod.Body;

            if (body.Instructions.Count == 0)
                throw CilMain.Where.Exception("input method is empty");

            if (body.Instructions[body.Instructions.Count - 1].Offset > 0xFFF0)
                throw CilMain.Where.Exception("input method is too large");

            CodeBuilder o = null;
            try
            {
                o = new CodeBuilder();

                o.defMethod = defMethod;
                o.defMethodBody = defMethod.Body;
                o.newMethod = newMethod;
                o.method = myMethod;

                o.Process(numCastableInterfaces);
            }
            catch (Exception e)
            {
                if (e is JavaException)
                    throw;
                #if DEBUGDIAG
                Console.WriteLine(e);
                #endif
                if (o != null && o.cilInst != null)
                {
                    if (o.lineNumber != 0)
                        CilMain.Where.Push($"line {o.lineNumber}");
                    CilMain.Where.Push(
                        $"'{o.cilInst.OpCode.Name}' instruction at offset {o.cilInst.Offset,0:X4}");
                }
                string msg = e.Message;
                if (e is InvalidProgramException)
                    msg = "unexpected opcode or operands";
                throw CilMain.Where.Exception(msg);
            }
        }



        void Process(int numCastableInterfaces)
        {
            code = newMethod.Code = new JavaCode(newMethod);
            var oldLabel = code.SetLabel(0xFFFF);

            locals = new CodeLocals(method, defMethod, code);
            arrays = new CodeArrays(code, locals);
            exceptions = new CodeExceptions(defMethodBody, code, locals);

            InsertMethodInitCode(numCastableInterfaces);

            code.SetLabel(oldLabel);

            stackMap = code.StackMap;
            stackMap.SaveFrame(0, false, CilMain.Where);

            ProcessInstructions();

            (code.MaxLocals, code.MaxStack) = locals.GetMaxLocalsAndStack();
            locals.TrackUnconditionalBranch(null);
        }



        void InsertMethodInitCode(int numCastableInterfaces)
        {
            if (method.IsStatic)
            {
                if ((! method.IsConstructor) && method.DeclType.HasGenericParameters)
                {
                    // in a static method in a generic class, if the class has
                    // a static initializer, then we want to start with a call to
                    // GetType(), to force initialization of the static data class

                    foreach (var m in defMethod.DeclaringType.Methods)
                    {
                        if (m.IsConstructor && m.IsStatic)
                        {
                            GenericUtil.LoadMaybeGeneric(method.DeclType, code);
                            code.NewInstruction(0x57 /* pop */, null, null);
                            code.StackMap.PopStack(CilMain.Where);
                            break;
                        }
                    }
                }
            }
            else if (method.IsConstructor)
            {
                if (method.DeclType.HasGenericParameters)
                {
                    // in a constructor of a generic class, we want to start
                    // with a call to GetType() and store the result in the
                    // $type field
                    GenericUtil.InitializeTypeField(method.DeclType, code);
                }

                // init the array of generic interfaces
                InterfaceBuilder.InitInterfaceArrayField(
                                    method.DeclType, numCastableInterfaces, code);

                // in any constructor, we want to allocate boxed instance fields
                ValueUtil.InitializeInstanceFields(newMethod.Class, method.DeclType,
                                                   defMethodBody.Instructions, code);
            }
        }



        void ProcessInstructions()
        {
            var sequencePoints = defMethod.DebugInformation.SequencePoints;
            int sequencePointIndex = 0;

            #if DEBUGDIAG
            Console.WriteLine();
            Console.WriteLine("METHOD " + defMethod + " [[ " + code.Method + " ]]");
            Console.WriteLine();
            #endif

            foreach (var cilInst in defMethodBody.Instructions)
            {
                while (    sequencePointIndex < sequencePoints.Count
                        && sequencePoints[sequencePointIndex].Offset <= cilInst.Offset)
                {
                    if (! sequencePoints[sequencePointIndex].IsHidden)
                    {
                        if (newMethod.Class.SourceFile == null)
                        {
                            var path = sequencePoints[sequencePointIndex].Document?.Url;
                            if (path != null)
                            {
                                newMethod.Class.SourceFile = System.IO.Path.GetFileName(path);
                            }
                        }
                        if (sequencePoints[sequencePointIndex].Offset >= cilInst.Offset)
                        {
                            var line2 = sequencePoints[sequencePointIndex].StartLine;
                            if (line2 > lineNumber)
                                lineNumber = line2;
                            break;
                        }
                    }
                    sequencePointIndex++;
                }

                ProcessOneInstruction(cilInst);
            }

            #if DEBUGDIAG
            if (newMethod.Class.SourceFile == null)
                newMethod.Class.SourceFile = "(bytecode)";
            #endif
        }



        void ProcessOneInstruction(Mono.Cecil.Cil.Instruction cilInst)
        {
            #if DEBUGDIAG
            var (stack1, stack2) = stackMap.FrameToString();
            Console.WriteLine($"LOCALS [{stack1}] STACK [{stack2}]");
            Console.WriteLine(cilInst);
            #endif

            int instructionsCount = code.Instructions.Count;

            code.SetLabel((ushort) cilInst.Offset);

            this.cilInst = cilInst;
            this.cilOp = cilInst.OpCode.Code;

            exceptions.CheckAndSaveFrame(cilInst);

            var genericMark = CilMain.GenericStack.Mark();
            CallInstructionTranslator();
            CilMain.GenericStack.Release(genericMark);

            while (instructionsCount < code.Instructions.Count)
            {
                var newInst = code.Instructions[instructionsCount++];
                #if DEBUGDIAG
                newInst.Line = (ushort) (lineNumber != 0 ? lineNumber : cilInst.Offset);
                #else
                newInst.Line = (ushort) lineNumber;
                #endif
            }
        }



        void CallInstructionTranslator()
        {
            switch (cilOp)
            {
                case Code.Nop:
                case Code.Volatile:
                case Code.Constrained:
                case Code.Unaligned:
                case Code.Readonly:
                case Code.Tail:

                    code.NewInstruction(0x00 /* nop */, null, null);
                    break;

                case Code.Break:

                    code.NewInstruction(0xCA /* debugger break */, null, null);
                    break;

                case Code.Pop:
                case Code.Dup:

                    PopOrDupStack(cilOp);
                    break;

                case Code.Ldarg_0:  case Code.Ldarg_1:  case Code.Ldarg_2:  case Code.Ldarg_3:
                case Code.Ldloc_0:  case Code.Ldloc_1:  case Code.Ldloc_2:  case Code.Ldloc_3:
                case Code.Ldarg:    case Code.Ldarg_S:  case Code.Ldloc:    case Code.Ldloc_S:

                    locals.LoadValue(cilOp, cilInst.Operand);
                    break;

                case Code.Stloc_0:  case Code.Stloc_1:  case Code.Stloc_2:  case Code.Stloc_3:
                case Code.Starg:    case Code.Starg_S:  case Code.Stloc:    case Code.Stloc_S:

                    locals.StoreValue(cilOp, cilInst.Operand);
                    break;

                case Code.Ldarga:   case Code.Ldloca:   case Code.Ldarga_S: case Code.Ldloca_S:

                    locals.LoadAddress(cilInst.Operand);
                    break;

                case Code.Ldc_I4_M1:case Code.Ldc_I4_0: case Code.Ldc_I4_1: case Code.Ldc_I4_2:
                case Code.Ldc_I4_3: case Code.Ldc_I4_4: case Code.Ldc_I4_5: case Code.Ldc_I4_6:
                case Code.Ldc_I4_7: case Code.Ldc_I4_8: case Code.Ldc_I4_S: case Code.Ldc_I4:
                case Code.Ldc_I8:   case Code.Ldc_R4:   case Code.Ldc_R8:
                case Code.Ldstr:    case Code.Ldnull:

                    LoadConstant(cilOp, cilInst.Operand);
                    break;

                case Code.Ldfld:    case Code.Ldflda:   case Code.Stfld:
                case Code.Ldsfld:   case Code.Ldsflda:  case Code.Stsfld:

                    LoadStoreField(cilOp, cilInst.Operand);
                    break;

                case Code.Initobj:

                    InitObject(cilInst.Operand);
                    break;

                case Code.Newobj:   case Code.Call:     case Code.Callvirt: case Code.Ret:

                    CallMethod(cilOp, cilInst.Operand);
                    break;

                case Code.Castclass:

                    CastToClass(cilInst.Operand);
                    break;

                case Code.Ldtoken:

                    LoadToken();
                    break;

                case Code.Unbox:    case Code.Unbox_Any:

                    UnboxObject(cilOp, cilInst.Operand);
                    break;

                case Code.Ldobj:    case Code.Box:

                    LoadObject(cilOp, cilInst.Operand);
                    break;

                case Code.Stobj:

                    StoreObject(cilInst.Operand);
                    break;

                case Code.Br:       case Code.Br_S:
                case Code.Brtrue:   case Code.Brtrue_S: case Code.Beq:      case Code.Beq_S:
                case Code.Bgt:      case Code.Bgt_S:    case Code.Bgt_Un:   case Code.Bgt_Un_S:
                case Code.Blt:      case Code.Blt_S:    case Code.Blt_Un:   case Code.Blt_Un_S:

                    CodeCompare.Straight(code, cilOp, locals, cilInst);
                    break;

                case Code.Brfalse:  case Code.Brfalse_S:case Code.Bne_Un:   case Code.Bne_Un_S:
                case Code.Ble:      case Code.Ble_S:    case Code.Ble_Un:   case Code.Ble_Un_S:
                case Code.Bge:      case Code.Bge_S:    case Code.Bge_Un:   case Code.Bge_Un_S:

                    CodeCompare.Opposite(code, cilOp, locals, cilInst);
                    break;

                case Code.Cgt:      case Code.Cgt_Un:                       case Code.Ceq:
                case Code.Clt:      case Code.Clt_Un:

                    CodeCompare.Compare(code, cilOp, cilInst);
                    break;

                case Code.Switch:

                    CodeCompare.Switch(code, cilInst);
                    break;

                case Code.Isinst:

                    CodeCompare.Instance(code, locals, cilInst);
                    break;

                case Code.Conv_I1:  case Code.Conv_Ovf_I1:  case Code.Conv_Ovf_I1_Un:
                case Code.Conv_I2:  case Code.Conv_Ovf_I2:  case Code.Conv_Ovf_U2_Un:
                case Code.Conv_I4:  case Code.Conv_Ovf_I4:  case Code.Conv_Ovf_I4_Un:
                case Code.Conv_I8:  case Code.Conv_Ovf_I8:  case Code.Conv_Ovf_I8_Un:
                case Code.Conv_U1:  case Code.Conv_Ovf_U1:  case Code.Conv_Ovf_U1_Un:
                case Code.Conv_U2:  case Code.Conv_Ovf_U2:  case Code.Conv_Ovf_I2_Un:
                case Code.Conv_U4:  case Code.Conv_Ovf_U4:  case Code.Conv_Ovf_U4_Un:
                case Code.Conv_U8:  case Code.Conv_Ovf_U8:  case Code.Conv_Ovf_U8_Un:
                case Code.Conv_I:   case Code.Conv_Ovf_I:   case Code.Conv_Ovf_I_Un:
                case Code.Conv_U:   case Code.Conv_Ovf_U:   case Code.Conv_Ovf_U_Un:
                case Code.Conv_R4:  case Code.Conv_R8:      case Code.Conv_R_Un:

                    CodeNumber.Conversion(code, cilOp);
                    break;

                case Code.Add:      case Code.Sub:      case Code.Mul:      case Code.Neg:
                case Code.Div:      case Code.Div_Un:   case Code.Rem:      case Code.Rem_Un:
                case Code.And:      case Code.Or:       case Code.Xor:      case Code.Not:
                case Code.Shl:      case Code.Shr:      case Code.Shr_Un:
                case Code.Add_Ovf:                      case Code.Add_Ovf_Un:
                case Code.Sub_Ovf:                      case Code.Sub_Ovf_Un:
                case Code.Mul_Ovf:                      case Code.Mul_Ovf_Un:

                    CodeNumber.Calculation(code, cilOp);
                    break;

                case Code.Ldind_I1: case Code.Ldind_U1: case Code.Ldind_I2: case Code.Ldind_U2:
                case Code.Ldind_I4: case Code.Ldind_U4: case Code.Ldind_I8: case Code.Ldind_I:
                case Code.Ldind_R4: case Code.Ldind_R8: case Code.Ldind_Ref:
                case Code.Stind_I1: case Code.Stind_I2: case Code.Stind_I4: case Code.Stind_I8:
                case Code.Stind_I:  case Code.Stind_R4: case Code.Stind_R8: case Code.Stind_Ref:

                    CodeNumber.Indirection(code, cilOp);
                    break;

                case Code.Throw:    case Code.Rethrow:  case Code.Leave:    case Code.Leave_S:
                case Code.Endfinally:                   case Code.Endfilter:

                    exceptions.Translate(cilInst);
                    break;

                case Code.Newarr:

                    arrays.New(cilInst.Operand);
                    break;

                case Code.Ldlen:

                    arrays.Length();
                    break;

                case Code.Ldelema:

                    arrays.Address(null, cilInst);
                    break;

                case Code.Ldelem_I1:case Code.Ldelem_U1:case Code.Ldelem_I2:case Code.Ldelem_U2:
                case Code.Ldelem_I4:case Code.Ldelem_U4:case Code.Ldelem_I8:case Code.Ldelem_I:
                case Code.Ldelem_R4:case Code.Ldelem_R8:case Code.Ldelem_Ref:case Code.Ldelem_Any:

                    arrays.Load(cilOp, cilInst.Operand, cilInst);
                    break;

                case Code.Stelem_I1:case Code.Stelem_I2:case Code.Stelem_I4:case Code.Stelem_I8:
                case Code.Stelem_I: case Code.Stelem_R4:case Code.Stelem_R8:case Code.Stelem_Any:
                case Code.Stelem_Ref:

                    arrays.Store(cilOp);
                    break;

                case Code.Ldftn: case Code.Ldvirtftn:

                    Delegate.LoadFunction(code, cilInst);
                    break;

                case Code.Sizeof:

                    CodeSpan.Sizeof(cilInst.Operand, code);
                    break;

                case Code.Localloc:

                    CodeSpan.Localloc(code);
                    break;

                /*  instructions not handled:
                        Jmp, Calli, Cpobj, Refanyval, Ckfinite, Mkrefany, Arglist,
                        Tail, Cpblk, Initblk, No, Refanytype, */

                default:
                    throw new InvalidProgramException();
            }
        }

    }
}
