
using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public partial class CodeBuilder
    {

        void LoadStoreField(Code op, object data)
        {
            bool ok = false;

            if (data is FieldReference fieldRef)
            {
                var fldName  = CilMain.MakeValidMemberName(fieldRef.Name);
                var fldClass = CilMain.GenericStack.EnterType(fieldRef.DeclaringType);
                var fldType  = ValueUtil.GetBoxedFieldType(fldClass, fieldRef);

                if (    fldClass.Equals(JavaType.StringType)
                     || fldClass.Equals(JavaType.ThrowableType))
                {
                    // generally we translate System.String to java.lang.String,
                    // and System.Exception to java.lang.Throwable,
                    // but not in the case of a field reference
                    fldClass = CilType.From(new JavaType(0, 0, fldClass.JavaName));
                }

                bool isLoad = (op != Code.Stfld && op != Code.Stsfld);
                bool isStatic = (op == Code.Ldsfld || op == Code.Ldsflda || op == Code.Stsfld);
                bool isAddress = (op == Code.Ldflda || op == Code.Ldsflda);
                bool isVolatile = CheckVolatile(fldType);

                if (isLoad)
                {
                    if (isAddress)
                        ok = LoadFieldAddress(fldName, fldType, fldClass, isStatic);
                    else
                        ok = LoadFieldValue(fldName, fldType, fldClass, isStatic, isVolatile);
                }
                else
                    ok = StoreFieldValue(fldName, fldType, fldClass, isStatic, isVolatile);
            }

            if (! ok)
                throw new InvalidProgramException();
        }



        bool CheckVolatile(CilType fldType)
        {
            bool isVolatile = (    cilInst.Previous != null
                                && cilInst.Previous.OpCode.Code == Code.Volatile);

            if (fldType is BoxedType boxedType)
            {
                // all boxed fields are accessible as volatile
                return isVolatile || boxedType.UnboxedType.IsVolatile;
            }

            if ((! fldType.IsValueClass) && (! fldType.IsGenericParameter))
            {
                // [java.attr.RetainType] volatile field
                return isVolatile || fldType.IsVolatile;
            }

            if (isVolatile)
                throw new Exception("unsupported type for volatile access");
            return false;
        }



        bool LoadFieldAddress(string fldName, CilType fldType, CilType fldClass, bool isStatic)
        {
            if (! (fldType is BoxedType))
            {
                if (! fldType.IsValueClass)
                    throw CilMain.Where.Exception($"cannot load address of unboxed field '{fldName}'");
            }

            if (isStatic && fldClass.HasGenericParameters)
            {
                fldClass = LoadStaticData(fldClass);
                isStatic = false;
            }

            byte op;
            if (! isStatic)
            {
                PopObjectAndLoadFromSpan(fldClass);
                op = 0xB4; // getfield
            }
            else
                op = 0xB2; // getstatic

            code.NewInstruction(op, fldClass.AsWritableClass, new JavaFieldRef(fldName, fldType));

            if (fldType is ThreadBoxedType tlsType)
                tlsType.GetInnerObject(code);

            if (fldType.IsGenericParameter)
                fldType = GenericUtil.CastMaybeGeneric(fldType, true, code);

            stackMap.PushStack(fldType);

            return true;
        }



        bool LoadFieldValue(string fldName, CilType fldType, CilType fldClass,
                            bool isStatic, bool isVolatile)
        {
            if (isStatic && fldClass.HasGenericParameters)
            {
                fldClass = LoadStaticData(fldClass);
                isStatic = false;
            }

            byte op;
            if (! isStatic)
            {
                if (method.IsConstructor &&
                        LoadFieldInConstructor(fldName, fldType, fldClass))
                    return true;

                PopObjectAndLoadFromSpan(fldClass);
                op = 0xB4; // getfield
            }
            else
                op = 0xB2; // getstatic

            code.NewInstruction(op, fldClass.AsWritableClass, new JavaFieldRef(fldName, fldType));

            if (fldType is BoxedType boxedType)
            {
                boxedType.GetValue(code, isVolatile);
                fldType = boxedType.UnboxedType;
                if (fldType.IsReference)
                {
                    if (fldType.IsGenericParameter && fldType.IsArray)
                    {
                        fldType = CodeArrays.GenericArrayType;
                    }
                    else if (! fldType.Equals(JavaType.ObjectType))
                    {
                        code.NewInstruction(0xC0 /* checkcast */, fldType.AsWritableClass, null);
                    }
                }
            }
            else if (fldType.IsGenericParameter)
            {
                var newType = GenericUtil.CastMaybeGeneric(fldType, false, code);
                fldType = (newType != fldType) ? newType : CilType.From(JavaType.ObjectType);
            }

            stackMap.PushStack(fldType);

            return true;
        }



        void PopObjectAndLoadFromSpan(CilType fldClass)
        {
            var stackTop = stackMap.PopStack(CilMain.Where);
            stackMap.PushStack(stackTop);

            if (    fldClass.IsValueClass
                 && CodeSpan.LoadStore(true, (CilType) stackTop, null, fldClass, code))
            {
                stackMap.PopStack(CilMain.Where);
                code.NewInstruction(0xC0 /* checkcast */, fldClass, null);
            }

            // pop object reference
            stackMap.PopStack(CilMain.Where);
        }



        bool LoadFieldInConstructor(string fldName, CilType fldType, CilType fldClass)
        {
            // Java does not allow 'getfield' instructions on an 'uninitializedThis'
            // until the call to super constructor.  but in .Net this is permitted,
            // and the F# compiler generates such code in some cases.  we try to work
            // around this, by identifying the constructor parameter that was used in
            // an earlier 'putfield', and loading that, instead of doing 'getfield'.

            bool isUninitializedThisField =
                        (    method.IsConstructor && fldClass.Equals(method.DeclType)
                          && stackMap.GetLocal(0).Equals(JavaStackMap.UninitializedThis));
            if (! isUninitializedThisField)
                return false;

            // most recent instruction before the 'getfield'
            // should have been 'ldarg.0', translated to 'aload_0'
            int i = code.Instructions.Count - 1;
            if (i > 0 && code.Instructions[i].Opcode == 0x19 /* aload */
                      && code.Instructions[i].Data is int thisIndex
                      && thisIndex == 0
                      // note that we pop the stack here
                      && code.StackMap.PopStack(CilMain.Where)
                            .Equals(JavaStackMap.UninitializedThis))
            {
                // try to find an earlier 'putfield' instruction for the field
                while (i-- > 0)
                {
                    var inst = code.Instructions[i];
                    if (    inst.Opcode == 0xB5 /* putfield */
                         && method.DeclType.Equals(inst.Class))
                    {
                        var instField = (JavaFieldRef) inst.Data;
                        if (    fldName == instField.Name
                             && fldType.Equals(instField.Type))
                        {
                            // try to find the load instruction that was used
                            // to load the value, for that earlier 'putfield'
                            if (FindLoadLocal(i - 1, code.Instructions.Count - 1))
                                return true;
                        }
                    }
                }
            }
            throw new Exception("load from uninitialized this");


            bool FindLoadLocal(int prevIdx, int lastIdx)
            {
                var prevInst = code.Instructions[prevIdx];

                if (    prevIdx > 0 && prevInst.Opcode == 0xB8 /* invokestatic */
                     && (JavaMethodRef) prevInst.Data is JavaMethodRef prevMethod)
                {
                    if (prevMethod.Name == "Box")
                    {
                        // we possibly found the sequence used in boxing -
                        //      xload value, invokestatic Value.Box(), putfield

                        prevInst = code.Instructions[prevIdx - 1];
                    }
                    else if (   prevIdx > 5 && prevMethod.Name == "Copy"
                             && code.Instructions[prevIdx - 1].Opcode == 0x5A /* dup_x1 */
                             && code.Instructions[prevIdx - 2].Opcode == 0xB8 /* invokestatic */
                             && code.Instructions[prevIdx - 3].Opcode == 0x19 /* aload (type) */
                             && code.Instructions[prevIdx - 4].Opcode == 0xB8 /* invokestatic */
                             && code.Instructions[prevIdx - 5].Opcode == 0x19 /* aload (value) */)
                    {
                        // we possibly found the sequence used in generics -
                        //      aload (local to use for store value)
                        //      invokestatic Generic.Load
                        //      aload (generic type parameter)
                        //      invokestatic Generic.New
                        //      dup_x1
                        //      invokestatic Generic.Copy    <=== prevIdx
                        //      putfield
                        // (see also StoreInstance method in this module)

                        prevInst = code.Instructions[prevIdx - 5];
                    }
                }

                if (    prevInst.Opcode >= 0x15 /* iload, lload, fload, */
                     && prevInst.Opcode <= 0x19 /*        dload, aload  */
                     && prevInst.Data is int localIndex)
                {
                    // if the instruction before the 'putfield' is a load
                    // from local, and assuming this local is a parameter,
                    // then we can just load this local again, to replace
                    // a 'getfield' instruction that cannot access 'this'

                    code.Instructions[lastIdx].Opcode = prevInst.Opcode;
                    code.Instructions[lastIdx].Data = localIndex;
                    stackMap.PushStack(code.StackMap.GetLocal(localIndex));
                    return true;
                }

                return false;
            }
        }



        bool StoreFieldValue(string fldName, CilType fldType, CilType fldClass,
                             bool isStatic, bool isVolatile)
        {
            //
            // we generate a sequence of instructions based on the combination of
            //      isStatic, isGeneric, isBoxed, isCopyable
            //
            // isStatic=1 IsGeneric=1 IsBoxed=1 IsCopyable=X:
            //   [VAL] -> LoadST -> [VAL] [ST] -> getfield -> [VAL] [BOX] -> BoxedType.SetVO
            //
            // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=1:
            //   [VAL] -> LoadST -> [VAL] [ST] -> getfield -> [VAL] [OBJ] -> ValueType.Copy
            //
            // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=0:
            // * [VAL] -> LoadST -> [VAL] [ST] -> swap -> [ST] [VAL] -> putfield
            //
            // isStatic=1 IsGeneric=0 IsBoxed=1 IsCopyable=X:
            //   [VAL] -> getstatic -> [VAL] [BOX] -> BoxedType.SetVO
            //
            // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=1:
            //   [VAL] -> getstatic -> [VAL] [OBJ] -> ValueType.Copy
            //
            // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=0:
            //   [VAL] -> putstatic
            //
            // isStatic=0 IsGeneric=X IsBoxed=1 IsCopyable=X:
            // * [INS] [VAL] -> swap -> [VAL] [INS] -> getfield -> [VAL] [BOX] -> BoxedType.SetOV
            //
            // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=1:
            //   [INS] [VAL] -> swap -> [VAL] [INS] -> getfield -> [VAL] [OBJ] -> ValueType.Copy
            //
            // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=0:
            //   [INS] [VAL] -> putfield
            //
            // the combinations marked with an asterisk require swapping operands,
            // which is handled more effectively by first popping the value operand
            //

            var fldRef = new JavaFieldRef(fldName, fldType);
            bool isBoxed = fldType is BoxedType;

            if (isStatic)
            {
                if (fldClass.HasGenericParameters)
                {
                    // isStatic=1 IsGeneric=1 IsBoxed=1 IsCopyable=X
                    // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=1
                    // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=0
                    StoreStaticGeneric(fldClass, fldType, fldRef, isVolatile);
                }
                else
                {
                    // isStatic=1 IsGeneric=0 IsBoxed=1 IsCopyable=X
                    // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=1
                    // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=0
                    StoreStaticRegular(fldClass, fldType, fldRef, isVolatile);
                }
            }
            else
            {
                // isStatic=0 IsGeneric=X IsBoxed=1 IsCopyable=X
                // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=1
                // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=0
                StoreInstance(fldClass, fldType, fldRef, isVolatile);
            }

            return true;



            void StoreStaticGeneric(CilType fldClass, CilType fldType, JavaFieldRef fldRef,
                                    bool isVolatile)
            {
                // isStatic=1 IsGeneric=1 IsBoxed=1 IsCopyable=X:
                //   [VAL] -> LoadST -> [VAL] [ST] -> getfield -> [VAL] [BOX] -> BoxedType.SetVO
                //
                // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=1:
                //   [VAL] -> LoadST -> [VAL] [ST] -> getfield -> [VAL] [OBJ] -> ValueType.Copy
                //
                // isStatic=1 IsGeneric=1 IsBoxed=0 IsCopyable=0:
                // * [VAL] -> LoadST -> [VAL] [ST] -> swap -> [ST] [VAL] -> putfield

                if (fldType.IsValueClass)
                {
                    fldClass = LoadStaticData(fldClass);

                    code.NewInstruction(0xB4 /* getfield */, fldClass.AsWritableClass, fldRef);

                    if (fldType is BoxedType boxedType && (! boxedType.IsBoxedIntPtr))
                        boxedType.SetValueVO(code, isVolatile);
                    else
                        GenericUtil.ValueCopy(fldType, code);
                }
                else
                {
                    var fldValue = stackMap.PopStack(CilMain.Where);
                    var localIndex = locals.GetTempIndex(fldValue);
                    code.NewInstruction(fldValue.StoreOpcode, null, localIndex);

                    fldClass = LoadStaticData(fldClass);

                    code.NewInstruction(fldValue.LoadOpcode, null, localIndex);
                    stackMap.PushStack(fldValue);
                    code.NewInstruction(0xB5 /* putfield */, fldClass.AsWritableClass, fldRef);

                    locals.FreeTempIndex(localIndex);
                }

                stackMap.PopStack(CilMain.Where);
                stackMap.PopStack(CilMain.Where);
            }



            void StoreStaticRegular(CilType fldClass, CilType fldType, JavaFieldRef fldRef,
                                    bool isVolatile)
            {
                // isStatic=1 IsGeneric=0 IsBoxed=1 IsCopyable=X:
                //   [VAL] -> getstatic -> [VAL] [BOX] -> BoxedType.SetVO
                //
                // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=1:
                //   [VAL] -> getstatic -> [VAL] [OBJ] -> ValueType.Copy
                //
                // isStatic=1 IsGeneric=0 IsBoxed=0 IsCopyable=0:
                //   [VAL] -> putstatic

                if (fldType.IsValueClass)
                {
                    var fldValue = stackMap.PopStack(CilMain.Where);
                    stackMap.PushStack(fldValue);

                    code.NewInstruction(0xB2 /* getstatic */, fldClass.AsWritableClass, fldRef);
                    stackMap.PushStack(fldType);

                    if (    fldType is BoxedType boxedType
                         && ((! boxedType.IsBoxedIntPtr) || (! fldValue.IsReference)))
                    {
                        // use SetValue for boxed types, except in the case of IntPtr,
                        // where we also require a primitive value on the stack
                        boxedType.SetValueVO(code, isVolatile);
                    }
                    else
                        GenericUtil.ValueCopy(fldType, code);

                    stackMap.PopStack(CilMain.Where);
                }
                else
                {
                    code.NewInstruction(0xB3 /* putstatic */, fldClass.AsWritableClass, fldRef);
                }

                stackMap.PopStack(CilMain.Where);
            }



            void StoreInstance(CilType fldClass, CilType fldType, JavaFieldRef fldRef,
                               bool isVolatile)
            {
                // isStatic=0 IsGeneric=X IsBoxed=1 IsCopyable=X:
                // * [INS] [VAL] -> swap -> [VAl] [INS] -> getfield -> [VAL] [BOX] -> BoxedType.SetOV
                //
                // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=1:
                // * [INS] [VAL] -> swap -> [VAL] [INS] -> getfield -> [VAL] [OBJ] -> ValueType.Copy
                //
                // isStatic=0 IsGeneric=X IsBoxed=0 IsCopyable=0:
                //   [INS] [VAL] -> putfield

                bool isUninitializedThisField =
                            (    method.IsConstructor && fldClass.Equals(method.DeclType)
                              && stackMap.GetLocal(0).Equals(JavaStackMap.UninitializedThis));

                if (fldType is BoxedType boxedType &&
                            (isUninitializedThisField || (! boxedType.IsBoxedIntPtr)))
                {
                    if (isUninitializedThisField)
                    {
                        // initializing boxed primitive value before call to
                        // base constructor:  'getfield' is not allowed at this point,
                        // but we can box a new object and 'putfield' it.
                        boxedType.BoxValue(code);
                        code.NewInstruction(0xB5 /* putfield */, fldClass.AsWritableClass, fldRef);
                    }
                    else
                    {
                        StoreIntoBoxedField(fldClass, boxedType, fldRef, isVolatile);
                    }
                }
                else if (fldType.IsValueClass && (! isUninitializedThisField))
                {
                    // note that if initializing a value type field before call
                    // to the base constructor, we select the next 'else' block.

                    if (fldType is BoxedType boxedType2 && boxedType2.IsBoxedIntPtr)
                    {
                        StoreIntoBoxedField(fldClass, boxedType2, fldRef, isVolatile);
                    }
                    else
                    {
                        code.NewInstruction(0x5F /* swap */, null, null);

                        var stackVal = stackMap.PopStack(CilMain.Where);
                        var stackObj = stackMap.PopStack(CilMain.Where);
                        stackMap.PushStack(stackObj);
                        stackMap.PushStack(stackVal);
                        if (CodeSpan.LoadStore(true, (CilType) stackObj, null, fldClass, code))
                        {
                            stackMap.PopStack(CilMain.Where);
                            code.NewInstruction(0xC0 /* checkcast */, fldClass, null);
                        }

                        code.NewInstruction(0xB4 /* getfield */, fldClass.AsWritableClass, fldRef);

                        GenericUtil.ValueCopy(fldType, code);
                    }
                }
                else
                {
                    // if this is a reference type, or a primitive type, we can
                    // do a simple 'putfield'.  we might also get here if this
                    // is a value class that is assigned before the call to the
                    // base constructor.

                    if (fldType.IsGenericParameter)
                    {
                        // if the field has a generic type, we first need to allocate
                        // it (as would be done by ValueUtil.ConstructValue) and then
                        // copy the value on the stack into it

                        GenericUtil.LoadMaybeGeneric(fldType, code);
                        code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                            new JavaMethodRef("New", CilType.SystemValueType, CilType.SystemTypeType));

                        // use dup_x1 to arrange the stack as (newObj, sourceObj, newObj)
                        code.NewInstruction(0x5A /* dup_x1 */, null, null);
                        stackMap.PushStack(fldType);
                        stackMap.PushStack(fldType);

                        // call GenericType.Copy(fromObj, toObj) in baselib
                        code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                                            new JavaMethod("Copy", JavaType.VoidType,
                                                    JavaType.ObjectType, JavaType.ObjectType));

                        stackMap.PopStack(CilMain.Where);
                        stackMap.PopStack(CilMain.Where);
                    }

                    else if (fldType.IsValueClass)
                    {
                        CilMethod.ValueMethod(CilMethod.ValueClone, code);
                        code.NewInstruction(0xC0 /* checkcast */, fldType.AsWritableClass, null);
                    }

                    code.NewInstruction(0xB5 /* putfield */, fldClass.AsWritableClass, fldRef);
                }

                stackMap.PopStack(CilMain.Where);
                stackMap.PopStack(CilMain.Where);
            }
        }



        void StoreIntoBoxedField(CilType fldClass, BoxedType fldBoxedType,
                                 JavaFieldRef fldRef, bool isVolatile)
        {
            var stackVal = stackMap.PopStack(CilMain.Where);
            var stackObj = stackMap.PopStack(CilMain.Where);
            stackMap.PushStack(stackObj);
            stackMap.PushStack(stackVal);

            var localIndex = locals.GetTempIndex(stackVal);
            code.NewInstruction(stackVal.StoreOpcode, null, localIndex);

            if (CodeSpan.LoadStore(true, (CilType) stackObj, null, fldClass, code))
            {
                stackMap.PopStack(CilMain.Where);
                code.NewInstruction(0xC0 /* checkcast */, fldClass, null);
            }

            code.NewInstruction(0xB4 /* getfield */, fldClass.AsWritableClass, fldRef);
            code.NewInstruction(stackVal.LoadOpcode, null, localIndex);
            fldBoxedType.SetValueOV(code, isVolatile);

            locals.FreeTempIndex(localIndex);
        }



        CilType LoadStaticData(CilType fldClass)
        {
            // static data of a generic type is not actually static,
            // see also GenericUtil.MakeGenericClass

            if (method.IsStatic && method.IsConstructor)
            {
                // if getstatic/putstatic for field in a generic type
                // is used in the static constructor of a generic type,
                // we can load local 0 as an optimization instead of
                // calling GenericUtil.LoadStaticData (because the static
                // constructor will become a normal constructor in the
                // type$static data class, see also GenericUtil)

                var declType = method.DeclType;
                int n1 = declType.GenericParametersCount;
                int n2 = fldClass.GenericParametersCount;
                if (n1 == n2 && fldClass.Equals(method.DeclType))
                {
                    for (int i = 0; i < n1; i++)
                    {
                        if (declType.GenericParameters[i].JavaName ==
                                                fldClass.GenericParameters[i].JavaName)
                        {
                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            return GenericUtil.PushStaticDataType(fldClass, code);
                        }
                    }
                }
            }

            // static data of a generic type is not actually static,
            // see also GenericUtil.MakeGenericClass

            return GenericUtil.LoadStaticData(fldClass, code);
        }

    }
}
