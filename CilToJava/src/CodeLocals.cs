
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CodeLocals
    {

        JavaCode code;
        JavaStackMap stackMap;

        int[] argToLocalMap;
        int[] varToLocalMap;
        CilType[] localTypes;

        //int indexLocalVars;
        //int countLocalVars;

        int maxTempIndex;
        int nextTempIndex;
        int nextTempLabel;

        int instCountAfterInitCode;

        List<ushort> unconditionalBranches;



        public CodeLocals(CilMethod myMethod, MethodDefinition defMethod, JavaCode code)
        {
            this.code = code;
            stackMap = code.StackMap = new JavaStackMap();

            InitLocals(myMethod, defMethod);
            WriteDebugData(myMethod.DeclType);
        }



        void InitLocals(CilMethod myMethod, MethodDefinition defMethod)
        {
            var _localTypes = new List<CilType>();

            int nextIndex = InitLocalsArgs(myMethod, _localTypes);
                //indexLocalVars = nextIndex;
                nextIndex = InitLocalsVars(defMethod.Body, _localTypes, nextIndex);
                //countLocalVars = nextIndex - indexLocalVars;

            maxTempIndex = nextIndex;
            nextTempIndex = nextIndex;
            nextTempLabel = 0xFFFE;

            InitLocalsRefs(defMethod, _localTypes);

            localTypes = _localTypes.ToArray();

            if (defMethod.Body.HasExceptionHandlers)
            {
                // jvm resets the stackmap on each exception clause, to the state it
                // was on entry to the try block.  this means any locals initialized
                // within a try block are 'lost' outside the try, and cause stackmap
                // conflicts.  two examples:
                //
                // (1) filter condition code (in a filter clause) sets a local to the
                // exception object, then uses that local in a separate catch clause.
                // (2) returning a value from a 'try' can initialize a local with the
                // return value, but the 'return' instruction is outside the 'try'.
                //
                // to avoid stackmap conflicts, we have to initialize the variables
                // in advance.  ideally, on entry to each 'try' block, and only for
                // those variables actually referenced in the block.  but this causes
                // several complications, so instead we go for a simpler approach,
                // and always initialize all variables in advance.

                InitializeUnusedVariables();
            }

            instCountAfterInitCode = code.Instructions.Count;
        }



        int InitLocalsArgs(CilMethod myMethod, List<CilType> localTypes)
        {
            var parameters = myMethod.WithGenericParameters.Parameters;
            int numArgs = parameters.Count;
            CilType thisType;

            if (myMethod.HasThisArg)
            {
                if (myMethod.IsConstructor)
                    thisType = CilType.From(JavaStackMap.UninitializedThis);
                else
                {
                    thisType = myMethod.DeclType;
                    if (thisType.IsValueClass)
                        thisType = thisType.MakeByRef();
                }
                stackMap.SetLocal(0, thisType);
                numArgs++;
            }
            else
                thisType = null;

            argToLocalMap = new int[numArgs];
            int nextArg = 0;
            int nextIndex = 0;

            if (thisType != null)
            {
                argToLocalMap[nextArg++] = nextIndex++;
                localTypes.Add(thisType);
                numArgs--;
            }

            for (int i = 0; i < numArgs; i++)
            {
                var argType = (CilType) parameters[i].Type;
                stackMap.SetLocal(nextIndex, argType);

                var genericType = argType.GetMethodGenericParameter();
                if (genericType != null)
                {
                    if (genericType.IsArray && genericType.IsGenericParameter)
                    {
                        // note that GenericArrayType is compared by reference
                        // in CodeArrays, to detect an array of a generic type T[]
                        argType = CodeArrays.GenericArrayType;
                    }
                    else
                        argType = genericType;
                }

                argToLocalMap[nextArg++] = nextIndex;
                nextIndex += argType.Category;

                localTypes.Add(argType);
                while (nextIndex > localTypes.Count)
                    localTypes.Add(null);
            }

            return nextIndex;
        }



        int InitLocalsVars(MethodBody cilBody, List<CilType> localTypes, int nextIndex)
        {
            int highestIndex = -1;
            if (cilBody.HasVariables)
            {
                foreach (var cilVar in cilBody.Variables)
                {
                    if (cilVar.Index > highestIndex)
                        highestIndex = cilVar.Index;
                }
            }

            varToLocalMap = new int[highestIndex + 1];

            if (cilBody.HasVariables)
            {
                foreach (var cilVar in cilBody.Variables)
                {
                    CilMain.Where.Push("local #" + cilVar.Index);

                    var genericMark = CilMain.GenericStack.Mark();
                    var varType = CilMain.GenericStack.EnterType(cilVar.VariableType);

                    if (varType.IsValueClass || varType.IsPointer)
                    {
                        bool isByReference = varType.IsByReference;
                        bool isPointer = varType.IsPointer;

                        if (isPointer)
                            varType = CilType.MakeSpanOf(varType);

                        // value classes are allocated at the top of the method
                        if ((! isByReference) || isPointer)
                        {
                            ValueUtil.InitLocal(varType, nextIndex, code);
                            varType = varType.MakeClonedAtTop();
                        }
                    }
                    else if (varType.IsByReference)
                        varType = new BoxedType(varType, false);
                    else if (varType.IsArray && varType.IsGenericParameter)
                    {
                        // note that GenericArrayType is compared by reference
                        // in CodeArrays, to detect an array of a generic type T[]
                        varType = CodeArrays.GenericArrayType;
                    }

                    varToLocalMap[cilVar.Index] = nextIndex;
                    nextIndex += varType.Category;

                    localTypes.Add(varType);
                    while (nextIndex > localTypes.Count)
                        localTypes.Add(null);

                    CilMain.GenericStack.Release(genericMark);
                    CilMain.Where.Pop();
                }
            }

            return nextIndex;
        }



        void InitLocalsRefs(MethodDefinition cilMethod, List<CilType> localTypes)
        {
            // if the method includes instructions that take the address of
            // a local (argument/variable), then:
            //
            // if it is a primitive value type, or a reference, then we have
            // to box that into a system.<primitive> or system.Reference.
            //
            // if it is a class value type, or a generic parameter, and if
            // it was not passed by reference, then we have to make a copy
            // of the incoming value.
            //
            // a pointer argument is actually passed as a span (value type)
            // so if it is modified, we have to make a copy, as above.
            //
            // (if it was a generic parameter which represents a value type,
            // then a copy was already made when the value was loaded.)

            var processed = new bool[localTypes.Count];

            foreach (var inst in cilMethod.Body.Instructions)
            {
                int index = -1;
                bool arg = false;
                bool isPointerSpan = false;

                var instOpc = inst.OpCode.Code;
                if (instOpc == Code.Ldarga || instOpc == Code.Ldarga_S)
                {
                    if (inst.Operand is ParameterDefinition param)
                    {
                        index = ArgumentIndex(param.Sequence);
                        arg = true;
                    }
                }
                else if (instOpc == Code.Starg || instOpc == Code.Starg_S)
                {
                    if (inst.Operand is ParameterDefinition param)
                    {
                        var paramType = param.ParameterType;
                        if (    (paramType.IsValueType && (! paramType.IsPrimitive))
                             || (isPointerSpan = paramType.IsPointer))
                        {
                            index = ArgumentIndex(param.Sequence);
                            arg = true;
                        }
                    }
                }
                else if (instOpc == Code.Ldloca || instOpc == Code.Ldloca_S)
                {
                    if (inst.Operand is VariableDefinition var)
                    {
                        index = VariableIndex(var.Index);
                        arg = false;
                    }
                }

                //
                // check if already processed, or does not require processing
                //

                if (index == -1 || processed[index])
                    continue;
                processed[index] = true;

                var plainType = localTypes[index];
                if (plainType.IsByReference)
                    continue;

                //
                // copy class value type argument, or generic parameter, if
                // it was indeed passed by value, because we know the method
                // is going to modify the value.  (this also applies to a
                // pointer, which is passed as a span, a value type object.)
                //

                if (    plainType.IsValueClass
                     || plainType.IsGenericParameter
                     || isPointerSpan)
                {
                    if (arg)
                    {
                        InitLocalsRefs2(plainType, null, true, index);

                        if (plainType.IsGenericParameter || isPointerSpan)
                        {
                            // must not set the stackmap for a generic parameter,
                            // see InitLocalsArgs for the difference between the
                            // stackmap and the localTypes array
                            localTypes[index] = plainType.MakeClonedAtTop();
                        }
                        else
                        {
                            var byrefType = plainType.MakeClonedAtTop().MakeByRef();
                            localTypes[index] = byrefType;
                            stackMap.SetLocal(index, byrefType);
                        }
                    }
                }
                else
                {
                    //
                    // otherwise the method is going to load the address of a local
                    // (either argument or variable) which is a primitive value, or
                    // a reference, so we have to box that local
                    //

                    // note that boxedType will be marked as if MakeClonedAtTop()
                    var boxedType = new BoxedType(plainType, true);
                    localTypes[index] = boxedType;
                    stackMap.SetLocal(index, boxedType);

                    InitLocalsRefs2(plainType, boxedType, arg, index);
                }
            }
        }



        void InitLocalsRefs2(CilType plainType, BoxedType boxedType, bool arg, int index)
        {
            if (arg)
                code.NewInstruction(plainType.LoadOpcode, null, index);
            else
                code.NewInstruction(plainType.InitOpcode, null, null);

            stackMap.PushStack(plainType);

            if (boxedType != null)
            {
                boxedType.BoxValue(code);
            }
            else if (plainType.IsGenericParameter)
            {
                GenericUtil.ValueClone(code);
            }
            else
            {
                CilMethod.ValueMethod(CilMethod.ValueClone, code);
                code.NewInstruction(0xC0 /* checkcast */, plainType, null);
            }

            code.NewInstruction(0x3A /* astore */, null, index);
            stackMap.PopStack(CilMain.Where);
        }



        int ArgumentIndex(int index)
        {
            if (index < 0 || index >= argToLocalMap.Length)
                throw new ArgumentException($"bad argument index {index}");
            return argToLocalMap[index];
        }



        int VariableIndex(int index)
        {
            if (index < 0 || index >= varToLocalMap.Length)
                throw new ArgumentException($"bad variable index {index}");
            return varToLocalMap[index];
        }



        public int GetTempIndex(JavaType type)
        {
            int index = nextTempIndex;
            stackMap.SetLocal(index, type);

            nextTempIndex += type.Category;
            if (nextTempIndex > maxTempIndex)
                maxTempIndex = nextTempIndex;

            return index;
        }



        public void FreeTempIndex(int index)
        {
            int category = stackMap.GetLocal(index).Category;
            stackMap.SetLocal(index, JavaStackMap.Top);
            if (nextTempIndex == index + category)
                nextTempIndex -= category;
        }



        public ushort GetTempLabel() => (ushort) (--nextTempLabel);



        //public (int, int) GetLocalsIndexAndCount() => (indexLocalVars, countLocalVars);



        public (int, int) GetMaxLocalsAndStack()
        {
            int maxLocals = maxTempIndex;
            int maxStack = stackMap.GetMaxStackSize(CilMain.Where);
            return (maxLocals, maxStack);
        }



        public void LoadValue(Code op, object data)
        {
            int localIndex;
            if (op >= Code.Ldarg_0 && op <= Code.Ldarg_3)
                localIndex = ArgumentIndex(op - Code.Ldarg_0);
            else if (op >= Code.Ldloc_0 && op <= Code.Ldloc_3)
                localIndex = VariableIndex(op - Code.Ldloc_0);
            else if (data is ParameterDefinition dataArg)
                localIndex = ArgumentIndex(dataArg.Sequence);
            else if (data is VariableDefinition dataVar)
                localIndex = VariableIndex(dataVar.Index);
            else
                throw new InvalidProgramException();

            var localType = localTypes[localIndex];
            if (stackMap.GetLocal(localIndex) == JavaStackMap.Top)
            {
                // it is legal CIL to load an uninitialized local, but we do
                // need to initialize the local to a default value for the jvm
                code.Instructions.Insert(instCountAfterInitCode,
                    new JavaCode.Instruction(
                        localType.StoreOpcode, null, localIndex, 0xFFFF));
                code.Instructions.Insert(instCountAfterInitCode,
                    new JavaCode.Instruction(
                        localType.InitOpcode, null, null, 0xFFFF));
                code.StackMap.SetLocalInAllFrames(
                        localIndex, localType, CilMain.Where);
            }
            code.NewInstruction(localType.LoadOpcode, null, localIndex);

            if (localType is BoxedType boxedType && boxedType.IsLocal)
            {
                boxedType.GetValue(code);               // extract actual value of boxed local
                var unboxedType = boxedType.UnboxedType;
                if (boxedType.IsBoxedReference && (! unboxedType.Equals(JavaType.ObjectType)))
                    code.NewInstruction(0xC0 /* checkcast */, unboxedType, null);

                stackMap.PushStack(unboxedType);
            }
            else
            {
                if (localType.IsGenericParameter && (! localType.IsByReference))
                {
                    GenericUtil.ValueLoad(code);
                    localType = CilType.From(JavaType.ObjectType);
                }

                stackMap.PushStack(localType);
            }
        }



        public void StoreValue(Code op, object data)
        {
            int localIndex;
            if (op >= Code.Stloc_0 && op <= Code.Stloc_3)
                localIndex = VariableIndex(op - Code.Stloc_0);
            else if (data is ParameterDefinition dataArg)
                localIndex = ArgumentIndex(dataArg.Sequence);
            else if (data is VariableDefinition dataVar)
                localIndex = VariableIndex(dataVar.Index);
            else
                throw new InvalidProgramException();

            var stackTop = (CilType) stackMap.PopStack(CilMain.Where);;
            stackMap.PushStack(stackTop);

            var localType = localTypes[localIndex];
            if (localType is BoxedType boxedType)
            {
                if (boxedType.IsLocal)
                {
                    code.NewInstruction(0x19 /* aload */, null, localIndex);
                    stackMap.PushStack(boxedType);
                    boxedType.SetValueVO(code);     // store actual value of boxed local
                    stackMap.PopStack(CilMain.Where);
                }
                else if (boxedType.IsBoxedReference)
                {
                    if (boxedType.UnboxedType.IsByReference)
                    {
                        // store address into by-reference variable
                        code.NewInstruction(0xC0 /* checkcast */, boxedType, null);
                        code.NewInstruction(localType.StoreOpcode, null, localIndex);
                    }
                    else
                    {
                        throw new Exception("check pinned");
                    }
                }
                else
                {
                    if (stackTop.Equals(JavaType.ObjectType))
                        code.NewInstruction(0xC0 /* checkcast */, boxedType, null);

                    if ((! stackTop.IsReference) &&
                                (    stackTop.PrimitiveType == TypeCode.Int64
                                  || stackTop.PrimitiveType == TypeCode.UInt64))
                    {
                        // typically, ldc.i4.0 followed by conv.u and stloc.x,
                        // in order to clear the byref variable of a 'fixed' statement
                        code.NewInstruction(0x58 /* pop2 */, null, null);
                        code.NewInstruction(0x01 /* aconst_null */, null, null);
                    }

                    // store into reference type
                    code.NewInstruction(localType.StoreOpcode, null, localIndex);
                    stackMap.SetLocal(localIndex, localType);
                }
            }
            else if (localType.IsValueClass && (! localType.IsByReference))
            {
                if (CodeSpan.Address(stackTop, localType, code))
                {
                    code.NewInstruction(localType.StoreOpcode, null, localIndex);
                }
                else
                {
                    // copying value type by value
                    code.NewInstruction(localType.LoadOpcode, null, localIndex);
                    stackMap.PushStack(localType);
                    GenericUtil.ValueCopy(localType, code);
                    stackMap.PopStack(CilMain.Where);
                }
            }
            else
            {
                // if storing into an array interface like IEnumerable
                // from an array on the stack, we need to create the proxy
                if (! CodeArrays.MaybeGetProxy(stackTop, localType, code))
                {
                    // if not an array, then it might be a span.
                    // box a span before storing into a by-reference variable
                    CodeSpan.Box(localType, stackTop, code);
                }

                // otherwise store into reference type
                code.NewInstruction(localType.StoreOpcode, null, localIndex);
                stackMap.SetLocal(localIndex, localType);
            }

            stackMap.PopStack(CilMain.Where);
        }



        public void LoadAddress(object data)
        {
            int localIndex;
            if (data is ParameterDefinition dataArg)
                localIndex = ArgumentIndex(dataArg.Sequence);
            else if (data is VariableDefinition dataVar)
                localIndex = VariableIndex(dataVar.Index);
            else
                throw new InvalidProgramException();

            var localType = localTypes[localIndex];
            if (! localType.IsReference)
                throw new InvalidProgramException();

            code.NewInstruction(0x19 /* aload */, null, localIndex);
            stackMap.PushStack(localType);
        }



        public void UpdateThis(CilType initializedClass)
        {
            stackMap.SetLocal(0, initializedClass);
            localTypes[0] = initializedClass;
        }



        public void InitializeUnusedVariables()
        {
            for (int i = 0; i < varToLocalMap.Length; i++)
            {
                int index = varToLocalMap[i];
                var type = stackMap.GetLocal(index);
                if (type.Equals(JavaStackMap.Top))
                {
                    type = localTypes[index];
                    code.NewInstruction(type.InitOpcode, null, null);
                    code.NewInstruction(type.StoreOpcode, null, index);
                    stackMap.SetLocal(index, type);
                }
            }
        }



        public List<(CilType, int)> GetUninitializedVariables()
        {
            List<(CilType, int)> list = null;
            for (int i = 0; i < varToLocalMap.Length; i++)
            {
                int index = varToLocalMap[i];
                var type = stackMap.GetLocal(index);
                if (index < nextTempIndex && type.Equals(JavaStackMap.Top))
                {
                    if (list == null)
                        list = new List<(CilType, int)>();
                    list.Add((localTypes[index], index));
                }
            }
            return list;
        }



        public (CilType, int) GetLocalFromLoadInst(Code op, object data)
        {
            int localIndex;
            if      (op >= Code.Ldloc_0 && op <= Code.Ldloc_3)
                localIndex = VariableIndex(op - Code.Ldloc_0);
            else if (op >= Code.Ldarg_0 && op <= Code.Ldarg_3)
                localIndex = ArgumentIndex(op - Code.Ldarg_0);
            else if (data is ParameterDefinition dataArg)
                localIndex = ArgumentIndex(dataArg.Sequence);
            else if (data is VariableDefinition dataVar)
                localIndex = VariableIndex(dataVar.Index);
            else
                return (null, -1);
            return (localTypes[localIndex], localIndex);
        }



        public (CilType, int) GetLocalFromStoreInst(Code op, object data)
        {
            int localIndex;
            if (op >= Code.Stloc_0 && op <= Code.Stloc_3)
                localIndex = VariableIndex(op - Code.Stloc_0);
            else if (data is ParameterDefinition dataArg)
                localIndex = ArgumentIndex(dataArg.Sequence);
            else if (data is VariableDefinition dataVar)
                localIndex = VariableIndex(dataVar.Index);
            else
                return (null, -1);
            return (localTypes[localIndex], localIndex);
        }



        public void TrackUnconditionalBranch(Mono.Cecil.Cil.Instruction inst)
        {
            // IL compilers may generate unreachable code beyond a 'goto' or
            // a 'throw', but if the unreachable instruction does not have a
            // stack frame, the jvm will report a verify error.  this method
            // tracks control transfer instructions during translation (when
            // called with a non-null inst).  at the end of the method (when
            // inst is null), it inserts any missing stack frames.

            if (inst != null)
            {
                // called from code translating an unconditional branch
                if ((inst = inst.Next) != null)
                {
                    var offset = (ushort) inst.Offset;
                    if (! stackMap.HasFrame(offset))
                    {
                        if (unconditionalBranches == null)
                            unconditionalBranches = new List<ushort>();
                        unconditionalBranches.Add(offset);
                    }
                }
            }
            else if (unconditionalBranches != null)
            {
                // called from CodeBuilder::Process() at method end
                foreach (var offset in unconditionalBranches)
                {
                    if (! stackMap.HasBranchFrame(offset))
                    {
                        stackMap.LoadFrame(offset, true, CilMain.Where);
                        stackMap.SaveFrame(offset, true, CilMain.Where);
                    }
                }
            }
        }



        void WriteDebugData(CilType declType)
        {
            var debug = new List<JavaFieldRef>();
            Append(argToLocalMap, "arg", declType, debug);
            // placing uninitialized locals in the table causes JDB to show
            // JDWP error 35.  to fix this, we have to track variables better.
            // Append(varToLocalMap, "var", declType, debug);
            code.DebugLocals = debug;

            void Append(int[] toLocalMap, string localNamePrefix,
                        CilType declType, List<JavaFieldRef> table)
            {
                for (int i = 0; i < toLocalMap.Length; i++)
                {
                    var type = localTypes[toLocalMap[i]];
                    if (type != null && (! type.Equals(JavaStackMap.Top)))
                    {
                        if (type.Equals(JavaStackMap.UninitializedThis))
                            type = declType;

                        var entry = new JavaFieldRef();
                        entry.Name = localNamePrefix + i.ToString();
                        entry.Type = type;
                        table.Add(entry);
                    }
                }
            }
        }

    }

}
