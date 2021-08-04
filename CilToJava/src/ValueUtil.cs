
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using CollectionOfInstructions = Mono.Collections.Generic.Collection<Mono.Cecil.Cil.Instruction>;

namespace SpaceFlint.CilToJava
{

    public static class ValueUtil
    {

        public static void MakeValueClass(JavaClass valueClass, CilType fromType,
                                          int numCastableInterfaces)
        {
            valueClass.Super = CilType.SystemValueType.ClassName;

            CreateDefaultConstructor(valueClass, fromType, numCastableInterfaces, true);
            CreateValueMethods(valueClass, fromType, numCastableInterfaces);

            if ((valueClass.Flags & JavaAccessFlags.ACC_ABSTRACT) == 0)
                valueClass.Flags |= JavaAccessFlags.ACC_FINAL;
        }



        //
        // create default parameterless constructor.  (it will be called at the
        // top of any method which allocates a value type local.)  and note that
        // if this is a generic value type, this new constructor will be further
        // modified in GenericUtil.MakeGenericClass.FixConstructorsInFrom
        //

        static void CreateDefaultConstructor(JavaClass valueClass, CilType fromType,
                                             int numCastableInterfaces, bool initFields)
        {
            foreach (var oldMethod in valueClass.Methods)
            {
                if (oldMethod.Name == "<init>")
                    return;
            }

            var code = CilMethod.CreateConstructor(valueClass, fromType.GenericParametersCount, true);

            if (fromType.HasGenericParameters)
            {
                code.StackMap = new JavaStackMap();
                var genericMark = CilMain.GenericStack.Mark();
                CilMain.GenericStack.EnterMethod(fromType, code.Method, true);

                // initialize the generic type field
                GenericUtil.InitializeTypeField(fromType, code);

                CilMain.GenericStack.Release(genericMark);
                code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);
            }

            // init the array of generic interfaces
            InterfaceBuilder.InitInterfaceArrayField(
                    fromType, numCastableInterfaces, code, 0);

            if (initFields)
            {
                var oldLabel = code.SetLabel(0xFFFF);

                InitializeInstanceFields(valueClass, fromType, null, code);

                code.SetLabel(oldLabel);
            }

            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.NewInstruction(0xB7 /* invokespecial */, new JavaType(0, 0, valueClass.Super),
                                new JavaMethodRef("<init>", JavaType.VoidType));

            code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);
            if (code.MaxStack < 1)
                code.MaxStack = 1;

            code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);
        }



        static void CreateValueMethods(JavaClass valueClass, CilType fromType,
                                       int numCastableInterfaces)
        {
            CreateValueClearMethod(valueClass, fromType);
            CreateValueCopyToMethod(valueClass, fromType);
            CreateValueCloneMethod(valueClass, fromType, numCastableInterfaces);

            //
            // system-ValueMethod-Clear() resets all fields to their default value
            //

            void CreateValueClearMethod(JavaClass valueClass, CilType fromType)
            {
                var code = CilMain.CreateHelperMethod(valueClass, CilMethod.ValueClear, 1, 2);

                if (valueClass.Fields != null && valueClass.Fields.Count != 0)
                {
                    foreach (var fld in valueClass.Fields)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) != 0)
                            continue;

                        if (fld.Type is CilType fldType && fldType.IsValueClass)
                        {
                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            code.NewInstruction(0xB4 /* getfield */, fromType, fld);
                            code.NewInstruction(0xB6 /* invokevirtual */,
                                                fldType, CilMethod.ValueClear);

                        }
                        else
                        {
                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            code.NewInstruction(fld.Type.InitOpcode, null, null);
                            code.NewInstruction(0xB5 /* putfield */, fromType, fld);
                        }
                    }
                }

                code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);
            }

            //
            // builds a system-ValueMethod-CopyTo() method, which takes a value
            // type object parameter, along with a 'this' object reference, and
            // copies each field from 'this' object to the other object
            //

            void CreateValueCopyToMethod(JavaClass valueClass, CilType fromType)
            {
                var code = CilMain.CreateHelperMethod(valueClass, CilMethod.ValueCopyTo, 2, 2);
                bool atLeastOneField = false;

                if (valueClass.Fields != null && valueClass.Fields.Count != 0)
                {
                    foreach (var fld in valueClass.Fields)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) != 0)
                            continue;

                        if (! atLeastOneField)
                        {
                            // cast the system.ValueType parameter to the actual type
                            code.NewInstruction(0x19 /* aload */, null, (int) 1);
                            code.NewInstruction(0xC0 /* checkcast */, fromType, null);
                            code.NewInstruction(0x3A /* astore */, null, (int) 1);
                            atLeastOneField = true;
                        }

                        if (fld.Type is CilType fldType && fldType.IsValueClass)
                        {
                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            code.NewInstruction(0xB4 /* getfield */, fromType, fld);
                            code.NewInstruction(0x19 /* aload */, null, (int) 1);
                            code.NewInstruction(0xB4 /* getfield */, fromType, fld);
                            code.NewInstruction(0xB6 /* invokevirtual */,
                                                fldType, CilMethod.ValueCopyTo);

                        }
                        else
                        {
                            code.NewInstruction(0x19 /* aload */, null, (int) 1);
                            code.NewInstruction(0x19 /* aload */, null, (int) 0);
                            code.NewInstruction(0xB4 /* getfield */, fromType, fld);
                            code.NewInstruction(0xB5 /* putfield */, fromType, fld);
                        }
                    }
                }

                code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);
            }

            //
            // system-ValueMethod-Clone() uses java.lang.Object.clone() to create
            // a new object with duplicate fields (which includes the generic type
            // field, if this is a generic type), and then calls itself again on
            // any boxed fields
            //

            void CreateValueCloneMethod(JavaClass valueClass, CilType fromType,
                                        int numCastableInterfaces)
            {
                var code = CilMain.CreateHelperMethod(valueClass, CilMethod.ValueClone,
                                              1, (numCastableInterfaces == 0) ? 3 : 5);
                bool atLeastOneField = false;

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0xB7 /* invokespecial */, JavaType.ObjectType,
                                    new JavaMethodRef("clone", JavaType.ObjectType));

                if (valueClass.Fields != null && valueClass.Fields.Count != 0)
                {
                    foreach (var fld in valueClass.Fields)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) != 0)
                            continue;

                        if (fld.Type is CilType fldType && fldType.IsValueClass)
                        {
                            if (! atLeastOneField)
                            {
                                code.NewInstruction(0xC0 /* checkcast */, fromType, null);
                                atLeastOneField = true;
                            }

                            code.NewInstruction(0x59 /* dup */, null, null);
                            code.NewInstruction(0x59 /* dup */, null, null);
                            code.NewInstruction(0xB4 /* getfield */, fromType, fld);
                            code.NewInstruction(0xB6 /* invokevirtual */, fldType, CilMethod.ValueClone);
                            code.NewInstruction(0xC0 /* checkcast */, fldType, null);
                            code.NewInstruction(0xB5 /* putfield */, fromType, fld);
                        }
                    }
                }

                if (! atLeastOneField)
                    code.NewInstruction(0xC0 /* checkcast */, fromType, null);

                if (numCastableInterfaces != 0)
                {
                    code.StackMap = new JavaStackMap();
                    // init the array of generic interfaces
                    InterfaceBuilder.InitInterfaceArrayField(
                        fromType, numCastableInterfaces, code, -1);
                }

                code.NewInstruction(fromType.ReturnOpcode, null, null);
            }
        }



        public static void MakeEnumClass(JavaClass enumClass, CilType enumType, bool isFlags)
        {
            ValueUtil.CreateDefaultConstructor(enumClass, enumType, 0, false);

            if (isFlags)
                enumClass.AddInterface("system.EnumFlags");

            //
            // create getter and setter
            //

            var theClass = new JavaType(0, 0, enumClass.Name);
            var theField = enumClass.Fields[0];

            var code = CilMain.CreateHelperMethod(enumClass,
                                    new JavaMethodRef("Get", JavaType.LongType), 1, 2);
            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.NewInstruction(0xB4 /* getfield */, theClass, theField);
            if (enumType.Category == 1)
                code.NewInstruction(0x85 /* i2l */, null, null);
            code.NewInstruction(code.Method.ReturnType.ReturnOpcode, null, null);

            code = CilMain.CreateHelperMethod(enumClass,
                                    new JavaMethodRef("Set", JavaType.VoidType), 3, 3);
            code.Method.Parameters.Add(new JavaFieldRef("", JavaType.LongType));
            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.NewInstruction(0x16 /* lload */, null, (int) 1);
            if (enumType.Category == 1)
                code.NewInstruction(0x88 /* l2i */, null, null);
            code.NewInstruction(0xB5 /* putfield */, theClass, theField);
            code.NewInstruction(code.Method.ReturnType.ReturnOpcode, null, null);
        }



        internal static void InitializeStaticFields(JavaClass theClass, CilType theType)
        {
            //
            // check if we have at least one value static field
            //

            if (theClass.Fields != null)
            {
                foreach (var fld in theClass.Fields)
                {
                    if (((CilType) fld.Type).IsValueClass)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) != 0)
                        {
                            CreateStaticFieldsInitializer(theClass, theType);
                            break;
                        }
                    }
                }
            }

            //
            // if any static fields are boxed, they must be allocated at the
            // top of the static constructor/initializer method.  if there is
            // no such method in the class, then we also have to create it.
            //

            void CreateStaticFieldsInitializer(JavaClass theClass, CilType theType)
            {
                JavaCode code = null;
                int numInstsInitially = 0;

                if (theClass.Methods == null)
                    theClass.Methods = new List<JavaMethod>();
                else
                {
                    foreach (var mth in theClass.Methods)
                    {
                        if (mth.Name == "<clinit>")
                        {
                            code = mth.Code;
                            numInstsInitially = code.Instructions.Count;
                            break;
                        }
                    }
                }

                if (code == null)
                {
                    code = CilMethod.CreateConstructor(theClass, theType.GenericParametersCount, false);
                }

                var oldLabel = code.SetLabel(0xFFFF);

                if (theType.HasGenericParameters)
                {
                    var genericMark = CilMain.GenericStack.Mark();
                    CilMain.GenericStack.EnterMethod(theType, code.Method, false);

                    code.NewInstruction(0x19 /* aload */, null, (int) 0);
                    var dataType = GenericUtil.PushStaticDataType(theType, code);

                    CreateGenericStaticFieldInitializer(theClass, dataType, code);

                    code.NewInstruction(0x57 /* pop */, null, null);
                    code.StackMap.PopStack(CilMain.Where);

                    CilMain.GenericStack.Release(genericMark);
                }
                else
                {
                    if (theClass.Name == "system.Type")
                    {
                        // inject a call at the top of the System.Type static initializer
                        // to initialize our system.RuntimeType class
                        code.NewInstruction(0xB8 /* invokestatic */, CilType.SystemRuntimeTypeType,
                                            new JavaMethodRef("StaticInit", JavaType.VoidType));
                    }

                    CreatePlainStaticFieldInitializer(theClass, theType, code);
                }

                if (numInstsInitially == 0)
                    code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null);
                else
                    CilMethod.MoveCodeFromBottomToTop(code, numInstsInitially);

                code.SetLabel(oldLabel);
                code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);

                //
                //
                //

                void CreatePlainStaticFieldInitializer(JavaClass theClass, CilType theType, JavaCode code)
                {
                    foreach (var fld in theClass.Fields)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) == 0)
                            continue;
                        if (! ((CilType) fld.Type).IsValueClass)
                            continue;

                        InitializeField(fld, code);

                        code.NewInstruction(0xB3 /* putstatic */, theType, fld);
                        code.StackMap.PopStack(CilMain.Where);
                    }
                }

                //
                //
                //

                void CreateGenericStaticFieldInitializer(JavaClass theClass, CilType theType, JavaCode code)
                {
                    foreach (var fld in theClass.Fields)
                    {
                        if ((fld.Flags & JavaAccessFlags.ACC_STATIC) == 0)
                            continue;
                        if (! ((CilType) fld.Type).IsValueClass)
                            continue;

                        code.NewInstruction(0x59 /* dup */, null, null);
                        code.StackMap.PushStack(theType);

                        InitializeField(fld, code);

                        code.NewInstruction(0xB5 /* putfield */, theType, fld);
                        code.StackMap.PopStack(CilMain.Where);
                        code.StackMap.PopStack(CilMain.Where);
                    }
                }
            }
        }



        internal static void InitializeInstanceFields(JavaClass theClass, CilType theType,
                                                      CollectionOfInstructions cilInsts, JavaCode code)
        {
            if (theClass.Fields == null || theClass.Fields.Count == 0)
                return;

            if (theType.HasGenericParameters)
            {
                bool instanceMethod = ((code.Method.Flags & JavaAccessFlags.ACC_STATIC) == 0);

                var genericMark = CilMain.GenericStack.Mark();
                CilMain.GenericStack.EnterMethod(theType, code.Method, instanceMethod);

                CreateInstanceFieldInitializer2(theClass, theType, code, cilInsts);

                CilMain.GenericStack.Release(genericMark);
            }
            else
                CreateInstanceFieldInitializer2(theClass, theType, code, cilInsts);

            //
            //
            //

            void CreateInstanceFieldInitializer2(JavaClass theClass, CilType theType, JavaCode code,
                                                 CollectionOfInstructions cilInsts)
            {
                foreach (var fld in theClass.Fields)
                {
                    if ((fld.Flags & JavaAccessFlags.ACC_STATIC) != 0)
                        continue;

                    var fldType2 = fld.Type as CilType;
                    if (! fldType2.IsValueClass)
                        continue;

                    if (cilInsts != null)
                    {
                        var thisName = theClass.Name;
                        var baseName = theClass.Super;
                        if (baseName == "system.Exception")
                            baseName = JavaType.ThrowableType.ClassName;

                        if (FieldHasInitialization(fld, thisName, baseName, cilInsts))
                            continue;
                    }

                    code.NewInstruction(0x19 /* aload */, null, (int) 0);
                    code.StackMap.PushStack(theType);

                    InitializeField(fld, code);

                    code.NewInstruction(0xB5 /* putfield */, theType, fld);
                    code.StackMap.PopStack(CilMain.Where);
                    code.StackMap.PopStack(CilMain.Where);
                }
            }

            //
            //
            //

            bool FieldHasInitialization(JavaField scanField,
                                        string thisName, string baseName,
                                        CollectionOfInstructions cilInsts)
            {
                // a constructor may begin by storing values into some field members,
                // before the base constructor was called.  at such an early point,
                // this must be translated to allocating new boxed objects and storing
                // them (see also:  StoreInstance in CodeField).
                //
                // note also that the code in the above CreateInstanceFieldInitializer2
                // calls InitializeField for every boxed field to allocate an object for
                // it.  this function attempts to detect and prevent the case where the
                // same field end up being initialized twice.

                foreach (var inst in cilInsts)
                {
                    if (inst.OpCode.Code == Code.Call || inst.OpCode.Code == Code.Callvirt)
                    {
                        // we stop scanning when we find a call to base constructor,
                        // or to some other constructor in the same class
                        if (inst.Operand is MethodReference mtd && mtd.Name == ".ctor")
                        {
                            var callName = CilMethod.From(mtd).DeclType.ClassName;
                            if (callName == thisName || callName == baseName)
                                break;
                        }
                    }

                    if (inst.OpCode.Code == Code.Stfld)
                    {
                        if (inst.Operand is FieldReference fld && fld.Name == scanField.Name)
                        {
                            if (CilType.From(fld.DeclaringType).ClassName == scanField.Class.Name)
                            {
                                // the 'stfld' we found will be translated to a Box() call
                                // followed by 'putfield', see StoreInstance in CodeField.
                                // which means we can skip initializing the field for now.
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }



        static void InitializeField(JavaField fld, JavaCode code)
        {
            // TypeBuilder.ImportFields stores a reference to the original CIL field
            var cilField = (FieldDefinition) fld.Constant;

            var fldType = (CilType) fld.Type;

            if (cilField.Constant == null)
            {
                var genericMark = CilMain.GenericStack.Mark();
                CilMain.GenericStack.EnterType(cilField.FieldType);

                ConstructValue(fldType, code);

                CilMain.GenericStack.Release(genericMark);
                return;
            }

            if ((! fldType.HasGenericParameters) && (fldType is BoxedType boxedType))
            {
                var unboxedType = boxedType.UnboxedType;
                object data = GetConstantData(unboxedType, cilField.Constant);
                if (data != null)
                {
                    code.NewInstruction(0x12 /* ldc */, null, data);
                    boxedType.BoxValue(code);
                    code.StackMap.PushStack(fldType);
                    return;
                }
            }

            throw new InvalidProgramException();

            //
            //
            //

            object GetConstantData(CilType unboxedType, object constant)
            {
                if (! unboxedType.IsReference)
                {
                    switch (unboxedType.PrimitiveType)
                    {
                        case TypeCode.Double:       return Convert.ToDouble(constant);
                        case TypeCode.Single:       return Convert.ToSingle(constant);
                        case TypeCode.UInt64:
                        case TypeCode.Int64:        return Convert.ToInt64(constant);
                        default:                    return Convert.ToInt32(constant);
                    }
                }
                else if (unboxedType.Equals(JavaType.StringType))
                {
                    return (constant as string);
                }
                return null;
            }
        }



        static void ConstructValue(CilType varType, JavaCode code)
        {
            ThreadBoxedType tlsType;

            if (varType is ThreadBoxedType)
            {
                // fields marked [ThreadStatic] are converted to fields
                // of type system.threading.ThreadLocal -- see baselib

                tlsType = (ThreadBoxedType) varType;

                code.NewInstruction(0xBB /* new */, tlsType, null);
                code.StackMap.PushStack(tlsType);
                code.NewInstruction(0x59 /* dup */, null, null);
                code.StackMap.PushStack(tlsType);

                varType = new BoxedType(tlsType.UnboxedType, false);
            }
            else
                tlsType = null;

            if (varType.IsGenericParameter)
            {
                GenericUtil.LoadMaybeGeneric(varType, code);
                code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                        new JavaMethodRef("New", CilType.SystemValueType, CilType.SystemTypeType));
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PushStack(varType);
                return;
            }

            code.NewInstruction(0xBB /* new */, varType, null);
            code.StackMap.PushStack(varType);

            code.NewInstruction(0x59 /* dup */, null, null);
            code.StackMap.PushStack(varType);

            var parameters = new List<JavaFieldRef>();
            if (varType.HasGenericParameters)
            {
                int n = 0;
                foreach (var arg in varType.GenericParameters)
                {
                    GenericUtil.LoadGeneric(arg.JavaName, code);
                    parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
                    n++;
                }
                while (n-- > 0)
                    code.StackMap.PopStack(CilMain.Where);
            }

            else if (varType.Equals(CodeSpan.SpanType) && varType.GenericParameters != null)
            {
                // pointer buffers are converted to the system.Span type, and
                // include the generic argument, but are not marked as generic.
                // see also:  CodeLocals::InitLocalsVars, CilType::MakeSpanOf

                GenericUtil.LoadMaybeGeneric(varType.GenericParameters[0], code);
                code.StackMap.PopStack(CilMain.Where);
                parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
            }

            var initMethod = new JavaMethodRef("<init>", JavaType.VoidType, parameters);

            code.NewInstruction(0xB7 /* invokespecial */, varType, initMethod);
            code.StackMap.PopStack(CilMain.Where);

            if (tlsType != null)
            {
                // complete the initialization of a [ThreadStatic] field
                // by calling the system.threading.ThreadLocal constructor

                code.NewInstruction(0xB7 /* invokespecial */, tlsType,
                                    new JavaMethodRef("<init>",
                                            JavaType.VoidType, CilType.SystemValueType));
                code.StackMap.PopStack(CilMain.Where);  // ValueType
                code.StackMap.PopStack(CilMain.Where);  // ThreadLocal reference
            }
        }



        internal static void InitLocal(CilType varType, int index, JavaCode code)
        {
            ConstructValue(varType, code);

            code.NewInstruction(0x3A /* astore */, null, (int) index);
            code.StackMap.PopStack(CilMain.Where);

            code.StackMap.SetLocal(index, varType);
        }



        internal static void CallInstanceFieldInitializer(CilType theType, CilMethod fromMethod,
                                                          JavaCode code)
        {
            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.StackMap.PushStack(theType);

            var parameters = new List<JavaFieldRef>();
            if (theType.HasGenericParameters)
            {
                int argumentIndex = 1;
                if (fromMethod != null)
                    argumentIndex += fromMethod.Parameters.Count - theType.GenericParameters.Count;

                int n = theType.GenericParameters.Count;
                for (int i = 0; i < n; i++)
                {
                    code.NewInstruction(0x19 /* aload */, null, (int) (argumentIndex + i));
                    code.StackMap.PushStack(JavaType.ClassType);
                    parameters.Add(new JavaFieldRef("", JavaType.ClassType));
                }
                for (int i = 0; i < n; i++)
                    code.StackMap.PopStack(CilMain.Where);
            }

            code.StackMap.PopStack(CilMain.Where);

            code.NewInstruction(0xB7 /* invokespecial */, theType,
                                new JavaMethodRef("-AllocFields", JavaType.VoidType, parameters));
        }



        internal static CilType GetBoxedFieldType(CilType fldClass, FieldReference fromField)
        {
            var fldType = CilType.From(fromField.FieldType);

            if (    (! fldType.IsValueClass)
                 && (fldClass == null || ! (fldClass.IsEnum || fldClass.IsRetainName)))
            {
                // value class type fields do not need to be boxed;
                // no fields are boxed if the class is an enum, or marked [RetainName]

                var defField = fromField.Resolve();

                if (defField == null)
                {
                    // we could not resolve the field, assume it should be boxed

                    fldType = new BoxedType(fldType, false);
                }
                else
                {
                    if (defField.IsLiteral)
                    {
                        // literal const fields do not need to be boxed

                        fldType = fldType.MakeLiteral();
                    }
                    else
                    {
                        if (fldType.IsPointer)
                            fldType = CilType.MakeSpanOf(fldType);

                        // fields marked [RetainType] should not be boxed, but fields
                        // marked [ThreadStatic] are boxed as system.threading.ThreadLocal

                        bool isThreadStatic = defField.IsStatic &&
                                defField.HasCustomAttribute("System.ThreadStaticAttribute", true);

                        if (! defField.HasCustomAttribute("RetainType"))
                        {
                            if (isThreadStatic)
                            {
                                if (defField.Constant != null)
                                {
                                    throw CilMain.Where.Exception(
                                        $"attribute [ThreadStatic] is incompatible with constant value");
                                }
                                fldType = new ThreadBoxedType(fldType);
                            }
                            else
                                fldType = new BoxedType(fldType, false);
                        }

                        else if (isThreadStatic)
                        {
                            throw CilMain.Where.Exception(
                                $"attributes [ThreadStatic] and [RetainType] are incompatible");
                        }
                    }
                }
            }

            return fldType;
        }

    }

}
