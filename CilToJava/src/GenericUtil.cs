
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;
using Instruction = SpaceFlint.JavaBinary.JavaCode.Instruction;

namespace SpaceFlint.CilToJava
{

    public static class GenericUtil
    {

        public static JavaClass MakeGenericClass(JavaClass fromClass, CilType fromType)
        {
            // if the generic class has static fields or a static initializer
            // then we need to move those into a separate class that can be
            // instantiated multiple times for multiple separate instances,
            // one for each concrete implementation of the generic type.

            int numGeneric = fromType.GenericParameters.Count;

            var dataClass = MoveStaticFields(fromClass, null);
                dataClass = MoveStaticInit(fromClass, dataClass);

            if (dataClass != null)
                FixConstructorInData(dataClass, numGeneric);

            // a generic class implements the IGenericObject interface,
            // and has a generic-type field for the concrete implementation
            // of the generic type and generic arguments

            CreateGenericTypeFields(fromClass, numGeneric);
            BuildGetTypeMethod(fromClass, fromType);

            return dataClass;

            //
            // move any static fields from the generic class,
            // as instance fields in a new class
            //

            JavaClass MoveStaticFields(JavaClass fromClass, JavaClass dataClass)
            {
                var fields = fromClass.Fields;
                if (fields == null)
                    return dataClass;

                int n = fields.Count;
                for (int i = 0; i < n; )
                {
                    var fld = fields[i];
                    if ((fld.Flags & JavaAccessFlags.ACC_STATIC) == 0)
                    {
                        i++;
                        continue;
                    }
                    if (((CilType) fld.Type).IsLiteral)
                    {
                        i++;
                        continue;
                    }

                    if (dataClass == null)
                        dataClass = CreateClass(fromClass);

                    if (fld.Constant != null)
                        throw CilMain.Where.Exception($"initializer in static field '{fld.Name}' in generic class");

                    fields.RemoveAt(i);
                    n--;

                    fld.Flags &= ~JavaAccessFlags.ACC_STATIC;
                    dataClass.Fields.Add(fld);
                }

                return dataClass;
            }

            //
            // move the static constructor/initializer
            // from the generic class to the new data class
            //

            JavaClass MoveStaticInit(JavaClass fromClass, JavaClass dataClass)
            {
                var methods = fromClass.Methods;
                int n = methods.Count;

                for (int i = 0; i < n; )
                {
                    var mth = methods[i];
                    if (mth.Name != "<clinit>")
                    {
                        i++;
                        continue;
                    }

                    if (dataClass == null)
                        dataClass = CreateClass(fromClass);

                    methods.RemoveAt(i);
                    n--;

                    mth.Name = "<init>";
                    mth.Class = dataClass;
                    mth.Flags = JavaAccessFlags.ACC_PUBLIC;
                    dataClass.Methods.Add(mth);
                }

                return dataClass;
            }

            //
            // create a constructor if there was no static initializer,
            // or inject a call to super class constructor
            //

            void FixConstructorInData(JavaClass dataClass, int numGeneric)
            {
                JavaCode code;
                bool insertReturn;

                if (dataClass.Methods.Count == 0)
                {
                    code = CilMethod.CreateConstructor(dataClass, numGeneric, true);
                    insertReturn = true;

                    code.MaxStack = 1;
                    code.MaxLocals = 1 + numGeneric;
                }
                else
                {
                    code = dataClass.Methods[0].Code;
                    if (code.MaxStack < 1)
                        code.MaxStack = 1;

                    insertReturn = false;
                }

                code.Instructions.Insert(0, new Instruction(
                        0x19 /* aload */, null, (int) 0, 0xFFFF));

                code.Instructions.Insert(1, new Instruction(
                        0xB7 /* invokespecial */, JavaType.ObjectType,
                        new JavaMethodRef("<init>", JavaType.VoidType), 0xFFFF));

                // the static initializer can call static methods on its own type,
                // and those methods can invoke system.RuntimeType.GetType() to get
                // a reference to the generic type that is still being initialized.
                // and more importantly, a reference to the the static-generic data
                // object that is constructed by this method.  to make the object
                // available to such access, we call system.RuntimeType.SetStatic().
                // see also system.RuntimeType.MakeGenericType/MakeGenericType().

                code.Instructions.Insert(2, new Instruction(
                        0x19 /* aload */, null, (int) 0, 0xFFFF));

                code.Instructions.Insert(3, new Instruction(
                        0xB8 /* invokestatic */, CilType.SystemRuntimeTypeType,
                        new JavaMethodRef("SetStatic",
                                            JavaType.VoidType, JavaType.ObjectType), 0xFFFF));

                if (insertReturn)
                {
                    code.NewInstruction(JavaType.VoidType.ReturnOpcode, null, null, 0xFFFF);
                }
            }

            //
            // create the new data class
            //

            JavaClass CreateClass(JavaClass fromClass) =>
                CilMain.CreateInnerClass(fromClass, fromClass.Name + "$$static", 0);

            //
            // create a private instance field to hold the runtime type
            // for a particular combination of generic type and arguments
            //

            void CreateGenericTypeFields(JavaClass fromClass, int numGeneric)
            {
                var fld = new JavaField();
                fld.Name = ConcreteTypeField.Name;
                fld.Type = ConcreteTypeField.Type;
                fld.Class = fromClass;
                fld.Flags = JavaAccessFlags.ACC_PRIVATE;

                if (fromClass.Fields == null)
                    fromClass.Fields = new List<JavaField>();

                fromClass.Fields.Add(fld);
            }
        }



        //
        // create the IGenericObject methods
        //

        public static void BuildGetTypeMethod(JavaClass theClass, CilType theType)
        {
            theClass.AddInterface("system.IGenericObject");

            var methodRef = new JavaMethodRef("system-IGenericObject-GetType",
                                              CilType.SystemTypeType);

            var code = CilMain.CreateHelperMethod(theClass, methodRef, 1, 1);

            if (theType.HasGenericParameters)
            {
                // this is a proper generic type with a -generic-type field
                // created by MakeGenericClass, which also invoked us.

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */, theType, ConcreteTypeField);
                code.NewInstruction(JavaType.ObjectType.ReturnOpcode, null, null);
            }
            else
            {
                // this is a non-generic type, but has a generic base type
                // which implements the GetType method, so we want to provide
                // an overriding implementation that returns the correct type.
                // we are invoked by TypeBuilder.

                code.StackMap = new JavaStackMap();
                LoadMaybeGeneric(theType, code);
                code.NewInstruction(JavaType.ObjectType.ReturnOpcode, null, null);
            }
        }



        //
        // create a java generic class signature, this is used by
        // system.RuntimeType in baselib to provide more accurate reflection
        //

        public static string MakeGenericSignature(TypeDefinition fromType, string superName)
        {
            string signature = "<";
            foreach (var prm in fromType.GenericParameters)
                signature += prm.Name + ":Ljava/lang/Object;";
            signature += ">" + ClassSignature(superName, fromType.BaseType);
            if (fromType.HasInterfaces)
            {
                foreach (var ifc in fromType.Interfaces)
                {
                    var name = CilType.From(ifc.InterfaceType).JavaName;
                    if (name != "system.ValueMethod")
                        signature += ClassSignature(name, ifc.InterfaceType);
                }
            }
            return signature;

            string ClassSignature(string className, TypeReference fromType)
            {
                var signature = "L" + className.Replace('.', '/');
                if (fromType is GenericInstanceType fromGenericType)
                {
                    signature += "<";
                    foreach (var arg in fromGenericType.GenericArguments)
                    {
                        if (arg is GenericParameter genericArg)
                            signature += "T" + arg.Name + ";";
                        else
                            signature += ClassSignature(CilType.From(arg).JavaName, arg);
                    }
                    signature += ">";
                }
                return signature + ";";
            }
        }



        //
        // create a dummy method that is used to extract information
        // about this generic type:  the number of type arguments
        // it takes, and the data class type.  this is used by the
        // CreateGeneric method in the system.RuntimeType constructor.
        //

        public static void CreateGenericInfoMethod(JavaClass theClass, JavaClass dataClass,
                                                   CilType fromType)
        {
            int numGeneric = fromType.GenericParameters.Count;
            var parameters = new List<JavaFieldRef>(numGeneric);
            for (int i = 0; i < numGeneric; i++)
                parameters.Add(new JavaFieldRef("", SystemGenericType));

            var (returnType, maxStack) = (dataClass == null)
                                       ? (JavaType.VoidType, 0)
                                       : (new JavaType(0, 0, dataClass.Name), 1);

            var methodRef = new JavaMethodRef("-generic-info-method", returnType, parameters);
            var code = CilMain.CreateHelperMethod(theClass, methodRef, numGeneric, maxStack);
            code.Method.Flags |= JavaAccessFlags.ACC_STATIC
                              |  JavaAccessFlags.ACC_FINAL
                              |  JavaAccessFlags.ACC_SYNTHETIC;

            if (dataClass != null)
                code.NewInstruction(0x01 /* aconst_null */, null, null);
            code.NewInstruction(returnType.ReturnOpcode, null, null);
        }



        //
        //
        //

        public static void CreateGenericVarianceField(JavaClass theClass, CilType fromType,
                                                      TypeDefinition defType)
        {
            // check if any of the generic parameters are variant.
            // note that generic parameter variance is only supported
            // on interfaces and delegates.

            if (! (fromType.IsInterface || fromType.IsDelegate))
                return;
            bool anyVariance = false;
            foreach (var gp in defType.GenericParameters)
            {
                if ((gp.Attributes & GenericParameterAttributes.VarianceMask) != 0)
                {
                    anyVariance = true;
                    break;
                }
            }
            if (! anyVariance)
                return;

            // build a string that describes the generic variance

            var chars = new char[defType.GenericParameters.Count];
            int idx = 0;
            foreach (var gp in defType.GenericParameters)
            {
                var v = gp.Attributes & GenericParameterAttributes.VarianceMask;
                chars[idx++] = (v == GenericParameterAttributes.Covariant)     ? 'O'
                             : (v == GenericParameterAttributes.Contravariant) ? 'I'
                                                                               : ' ';
            }

            var varianceField = new JavaField();
            varianceField.Name = "-generic-variance";
            varianceField.Type = JavaType.StringType;
            varianceField.Flags = JavaAccessFlags.ACC_STATIC
                                | JavaAccessFlags.ACC_FINAL
                                | JavaAccessFlags.ACC_PUBLIC
                                | JavaAccessFlags.ACC_TRANSIENT
                                | JavaAccessFlags.ACC_SYNTHETIC;
            varianceField.Constant = new string(chars);
            varianceField.Class = theClass;

            if (theClass.Fields == null)
                theClass.Fields = new List<JavaField>();
            theClass.Fields.Add(varianceField);
        }



        //
        // generate code to initialize the generic type field
        //

        public static void InitializeTypeField(CilType declType, JavaCode code)
        {
            code.NewInstruction(0x19 /* aload */, null, (int) 0);
            code.StackMap.PushStack(declType);

            LoadMaybeGeneric(declType, code);

            code.NewInstruction(0xC0 /* checkcast */, CilType.SystemRuntimeTypeType, null);
            code.NewInstruction(0xB5 /* putfield */, declType, ConcreteTypeField);

            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
        }



        public static CilType CastMaybeGeneric(CilType castType, bool valueOnly, JavaCode code)
        {
            if (! castType.IsGenericParameter)
                return castType;

            if (! valueOnly)
                GenericUtil.ValueLoad(code);

            var genericMark = CilMain.GenericStack.Mark();

            var (resolvedType, resolvedIndex) = CilMain.GenericStack.Resolve(castType.JavaName);

            if (resolvedIndex == 0)
            {
                if (valueOnly)
                {
                    // this flag is set when called from LoadFieldAddress.  we can cast
                    // the generic field to the actual type only if it is a value type
                    if (resolvedType.IsValueClass)
                    {
                        castType = resolvedType;
                        code.NewInstruction(0xC0 /* checkcast */, castType.AsWritableClass, null);
                    }
                    else if (castType.IsByReference)
                    {
                        var boxedType = new BoxedType(resolvedType, false);
                        code.NewInstruction(0xC0 /* checkcast */, boxedType, null);
                        castType = boxedType;
                    }
                }
                else
                {
                    // this flag is clear whe called from LoadFieldValue and
                    // PushMethodReturnType.  we can cast to any known actual types.

                    var arrayRank = castType.GetMethodGenericParameter()?.ArrayRank ?? 0;
                    castType = (arrayRank == 0) ? resolvedType : resolvedType.AdjustRank(arrayRank);

                    if (! castType.IsReference)
                    {
                        var boxedType = new BoxedType(castType, false);
                        code.NewInstruction(0xC0 /* checkcast */, boxedType, null);
                        boxedType.GetValue(code);
                    }
                    else
                    {
                        code.NewInstruction(0xC0 /* checkcast */, castType.AsWritableClass, null);
                    }
                }
            }

            CilMain.GenericStack.Release(genericMark);
            return castType;
        }



        public static void LoadGeneric(string loadName, JavaCode code)
        {
            var genericMark = CilMain.GenericStack.Mark();

            var (loadType, loadIndex) = CilMain.GenericStack.Resolve(loadName);
            LoadGeneric(loadType, loadIndex, code);

            CilMain.GenericStack.Release(genericMark);
        }



        static void LoadGeneric(CilType loadType, int loadIndex, JavaCode code)
        {
            if (loadIndex < 0)
            {
                // generic type is accessible through the generic-type member field

                code.NewInstruction(0x19 /* aload */, null, (int) 0);
                code.NewInstruction(0xB4 /* getfield */, loadType, ConcreteTypeField);
                code.StackMap.PushStack(ConcreteTypeField.Type);

                code.NewInstruction(0x12 /* ldc */, null, -loadIndex - 1);
                code.StackMap.PushStack(JavaType.IntegerType);

                // call system.RuntimeType.Argument(int typeArgumentIndex)
                code.NewInstruction(0xB6 /* invokevirtual */, CilType.SystemRuntimeTypeType,
                    new JavaMethodRef("Argument", CilType.SystemTypeType, JavaType.IntegerType));

                code.StackMap.PopStack(CilMain.Where);              // integer
                code.StackMap.PopStack(CilMain.Where);              // generic type field
                code.StackMap.PushStack(CilType.SystemTypeType);    // type result

            }
            else if (loadIndex > 0)
            {
                // generic type is accessible through a parameter

                code.NewInstruction(0x19 /* aload */, null, (int) (loadIndex - 1));
                code.StackMap.PushStack(CilType.SystemTypeType);
            }
            else
            {
                // generic type is known to be a constant

                LoadMaybeGeneric(loadType, code);
            }
        }



        static void LoadGenericInstance(CilType loadType, List<JavaFieldRef> parameters, JavaCode code)
        {
            int count = loadType.GenericParameters.Count;
            if (count == 1)
            {
                // specific handling for the common case of a generic instance
                // with just one type argument.  if this is a concrete argument,
                // call GetType(class, class).  if this is a generic argument,
                // call GetType(class, type).  note that the first class argument
                // to GetType was alredy inserted by our caller, LoadMaybeGeneric

                var genericMark = CilMain.GenericStack.Mark();

                var (argType, argIndex) = CilMain.GenericStack.Resolve(
                                            loadType.GenericParameters[0].JavaName);

                if (argIndex == 0 && (! argType.HasGenericParameters))
                {
                    // GetType(java.lang.Class, java.lang.Class)
                    code.NewInstruction(0x12 /* ldc */, argType.AsWritableClass, null);
                    code.StackMap.PushStack(CilType.ClassType);
                    // second parameter of type java.lang.Class
                    parameters.Add(parameters[0]);
                }
                else
                {
                    // GetType(java.lang.Class, system.Type)
                    LoadGeneric(argType, argIndex, code);
                    // second parameter of type system.Type
                    parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
                }

                CilMain.GenericStack.Release(genericMark);
            }
            else
            {
                // generic handling for the less common case of a generic instace
                // with more than one argument.  we don't check if the provided
                // type arguments are concrete or generic.  we build an array of
                // system.Type references, and call GetType(class, system.Type[]).

                var arrayOfType = CilType.SystemTypeType.AdjustRank(1);
                parameters.Add(new JavaFieldRef("", arrayOfType));

                code.NewInstruction(0x12 /* ldc */, null, count);
                code.StackMap.PushStack(JavaType.IntegerType);

                code.NewInstruction(0xBD /* anewarray */, CilType.SystemTypeType, null);
                code.StackMap.PopStack(CilMain.Where);
                code.StackMap.PushStack(arrayOfType);

                for (int i = 0; i < count; i++)
                {
                    code.NewInstruction(0x59 /* dup */, null, null);
                    code.StackMap.PushStack(arrayOfType);

                    code.NewInstruction(0x12 /* ldc */, null, i);
                    code.StackMap.PushStack(JavaType.IntegerType);

                    LoadMaybeGeneric(loadType.GenericParameters[i], code);

                    code.NewInstruction(0x53 /* aastore */, null, null);
                    code.StackMap.PopStack(CilMain.Where);
                    code.StackMap.PopStack(CilMain.Where);
                    code.StackMap.PopStack(CilMain.Where);
                }
            }
        }



        public static void LoadMaybeGeneric(CilType loadType, JavaCode code)
        {
            if (loadType.IsGenericParameter)
            {
                LoadGeneric(loadType.JavaName, code);
            }
            else
            {
                // all GetType variants take a first parameter of type java.lang.Class
                List<JavaFieldRef> parameters = new List<JavaFieldRef>();
                parameters.Add(new JavaFieldRef("", JavaType.ClassType));

                int arrayRank = loadType.ArrayRank;
                if (arrayRank == 0 || (! loadType.IsGenericParameter))
                    code.NewInstruction(0x12 /* ldc */, loadType.AsWritableClass, null);
                else
                {
                    // for a generic type T[], we want to load just the element T,
                    // and then (see below) call MakeArrayType on the result
                    var loadTypeElem = loadType.AdjustRank(-arrayRank);
                    code.NewInstruction(0x12 /* ldc */, loadTypeElem.AsWritableClass, null);
                }
                code.StackMap.PushStack(JavaType.ClassType);

                if (loadType.HasGenericParameters)
                {
                    LoadGenericInstance(loadType, parameters, code);
                }

                code.NewInstruction(0xB8 /* invokestatic */, CilType.SystemRuntimeTypeType,
                                    new JavaMethodRef("GetType", CilType.SystemTypeType, parameters));

                for (int i = 0; i < parameters.Count; i++)
                    code.StackMap.PopStack(CilMain.Where);

                code.StackMap.PushStack(CilType.SystemTypeType);

                if (arrayRank != 0 && loadType.IsGenericParameter)
                {
                    code.NewInstruction(0x12 /* ldc */, null, (int) arrayRank);
                    code.StackMap.PushStack(JavaType.IntegerType);

                    code.NewInstruction(0xB6 /* invokevirtual */, CilType.SystemTypeType,
                                        new JavaMethodRef("MakeArrayType",
                                                CilType.SystemTypeType, JavaType.IntegerType));

                    code.StackMap.PopStack(CilMain.Where);
                }
            }
        }



        public static void LoadParameterlessGeneric(CilType loadType, JavaCode code)
        {
            code.NewInstruction(0x12 /* ldc */, loadType.AsWritableClass, null);
            code.NewInstruction(0xB8 /* invokestatic */, CilType.SystemRuntimeTypeType,
                                new JavaMethodRef("GetType", CilType.SystemTypeType, JavaType.ClassType));
            code.StackMap.PushStack(CilType.SystemTypeType);
        }



        public static CilType LoadStaticData(CilType fldClass, JavaCode code)
        {
            LoadMaybeGeneric(fldClass, code);
            code.StackMap.PopStack(CilMain.Where);

            code.NewInstruction(0xB8 /* invokestatic */, CilType.SystemRuntimeTypeType,
                                new JavaMethodRef("GetStatic",
                                        JavaType.ObjectType, CilType.SystemTypeType));

            var returnType = PushStaticDataType(fldClass, code);
            code.NewInstruction(0xC0 /* checkcast */, returnType, null);

            return returnType;
        }



        public static CilType PushStaticDataType(CilType fldClass, JavaCode code)
        {
            var returnType = CilType.From(new JavaType(0, 0, fldClass.ClassName + "$$static"));
            code.StackMap.PushStack(returnType);
            return returnType;
        }



        public static void ValueLoad(JavaCode code)
        {
            code.NewInstruction(0xB8 /* invokestatic */, SystemGenericType, GenericLoad);
        }



        public static void ValueClone(JavaCode code)
        {
            code.NewInstruction(0xB8 /* invokestatic */, SystemGenericType, GenericClone);
        }



        public static void ValueCopy(CilType valueType, JavaCode code, bool swap = false)
        {
            // if 'from' value is pushed before 'into' object, call with swap == false
            // if 'into' object is pushed before 'from' value, call with swap == true
            if (valueType.IsGenericParameter)
            {
                if (swap)
                    code.NewInstruction(0x5F /* swap */, null, null);
                else
                {
                    // if storing a primitive value into a generic type,
                    // and the generic type can be resolved to a primitive type,
                    // then use a boxed-set method call

                    var stackArray = code.StackMap.StackArray();
                    int stackArrayLen = stackArray.Length;
                    if (stackArrayLen > 0 && (! stackArray[stackArrayLen - 1].IsReference))
                    {
                        var genericMark = CilMain.GenericStack.Mark();
                        var (primitiveType, _) = CilMain.GenericStack.Resolve(valueType.JavaName);
                        CilMain.GenericStack.Release(genericMark);

                        if (! primitiveType.IsReference)
                        {
                            var boxedType = new BoxedType(primitiveType, false);
                            code.NewInstruction(0xC0 /* checkcast */, boxedType, null);
                            boxedType.SetValueVO(code);
                            return;
                        }
                    }
                }

                code.NewInstruction(0xB8 /* invokestatic */, SystemGenericType,
                                    new JavaMethod("Copy", JavaType.VoidType,
                                            JavaType.ObjectType, JavaType.ObjectType));
            }
            else
            {
                var method = swap ? CilMethod.ValueCopyFrom : CilMethod.ValueCopyInto;
                CilMethod.ValueMethod(method, code);
            }
        }



        public static void CastToGenericType(TypeReference fromType, int throwBool, JavaCode code)
        {
            var genericMark = CilMain.GenericStack.Mark();
            GenericUtil.LoadMaybeGeneric(CilMain.GenericStack.EnterType(fromType), code);
            CilMain.GenericStack.Release(genericMark);

            code.NewInstruction(0x12 /* ldc */, null, throwBool);
            code.StackMap.PushStack(CilType.From(JavaType.IntegerType));

            var parameters = new List<JavaFieldRef>();
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
            parameters.Add(new JavaFieldRef("", JavaType.BooleanType));

            code.NewInstruction(0xB8 /* invokestatic */, GenericUtil.SystemGenericType,
                                new JavaMethodRef("TestCast",
                                        JavaType.ObjectType, parameters));
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
        }



        public static bool ShouldCallGenericCast(CilType fromType, CilType intoType)
        {
            if (object.ReferenceEquals(fromType, intoType))
                return false;   // no, if same type (by reference)

            if (intoType.PrimitiveType != 0 || intoType.ArrayRank != 0)
                return false;   // no, if casting to primitive or array

            if (intoType.HasGenericParameters)
            {
                if (intoType.Equals(CodeSpan.SpanType))
                    return false;
                return true;    // yes, if type is generic
            }

            if (fromType.Equals(intoType))
                return false;   // no, if same type (by value)

            // if casting to one of the types that any array should implememt,
            // and the castee is an array, or 'object', or one of those types,
            // then always call TestCast/CallCast, because we might have to
            // create a helper proxy for an array object

            bool fromTypeMayBeArray = (    fromType.ArrayRank != 0
                                        || fromType.Equals(JavaType.ObjectType)
                                        || IsArray(fromType.JavaName));

            return (fromTypeMayBeArray && IsArray(intoType.JavaName));

            bool IsArray(string clsnm) => (
                        clsnm == "system.Array"
                     || clsnm == "system.ICloneable"
                     || clsnm == "system.collections.IEnumerable"
                     || clsnm == "system.collections.ICollection"
                     || clsnm == "system.collections.IList"
                     || clsnm == "system.collections.IStructuralComparable"
                     || clsnm == "system.collections.IStructuralEquatable");

            #if false
            // if casting to an array, from System.Object, System.Array,
            // or one of the interface types that any array should implement,
            // then always call TestCast/CallCast, because we might have to
            // "unbox" the proxy (see below), and extract the array
            if (intoType.ArrayRank != 0)
            {
                if (fromType.ArrayRank != 0)
                    return false;   // no, if casting from array to array

                if (    fromType.Equals(JavaType.ObjectType)
                     || IsArray(fromType.JavaName)
                     || IsGenericArray(fromType.JavaName))
                    return true;    // yes, if casting
            }
            bool IsGenericArray(string clsnm) => (
                        clsnm == "system.collections.generic.IEnumerable$$1"
                     || clsnm == "system.collections.generic.ICollection$$1"
                     || clsnm == "system.collections.generic.IList$$1"
                     || clsnm == "system.collections.generic.IReadOnlyList$$1");
            #endif
        }



        internal static readonly JavaFieldRef ConcreteTypeField =
                                    new JavaFieldRef("-generic-type", CilType.SystemRuntimeTypeType);

        internal static readonly JavaType SystemGenericType =
                                    new JavaType(0, 0, "system.GenericType");

        internal static readonly JavaMethodRef GenericLoad =
                                    new JavaMethod("Load", JavaType.ObjectType, JavaType.ObjectType);

        internal static readonly JavaMethodRef GenericClear =
                                    new JavaMethod("Clear", JavaType.VoidType, JavaType.ObjectType);

        internal static readonly JavaMethodRef GenericClone =
                                    new JavaMethod("Clone", JavaType.ObjectType, JavaType.ObjectType);
    }

}
