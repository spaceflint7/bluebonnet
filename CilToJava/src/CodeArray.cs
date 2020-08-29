
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CodeArrays
    {

        CodeLocals locals;
        JavaCode code;
        JavaStackMap stackMap;



        public CodeArrays(JavaCode _code, CodeLocals _locals)
        {
            locals = _locals;
            code = _code;
            stackMap = code.StackMap;
        }



        public void Length()
        {
            var stackTop = (CilType) stackMap.PopStack(CilMain.Where);
            if (object.ReferenceEquals(stackTop, GenericArrayType))
            {
                // length of a generic array T[]
                code.NewInstruction(0xB8 /* invokestatic */,
                        new JavaType(0, 0, "java.lang.reflect.Array"),
                        new JavaMethodRef("getLength", JavaType.IntegerType, JavaType.ObjectType));
            }
            else if (stackTop.IsArray)
                code.NewInstruction(0xBE /* arraylength */, null, null);
            else
                throw new InvalidProgramException();
            stackMap.PushStack(CilType.From(JavaType.IntegerType));
        }



        public void New(object data)
        {
            if (data is TypeReference cilType)
            {
                var elemType = CilMain.GenericStack.EnterType(cilType);
                bool arrayTypeOnStack = false;

                int numDims = 0;
                while (data is ArrayType dataArrayType)
                {
                    numDims += dataArrayType.Rank;
                    data = dataArrayType.ElementType;
                }

                if (numDims > 0)
                {
                    if (data is GenericParameter)
                    {
                        GenericUtil.LoadMaybeGeneric(elemType, code);

                        code.NewInstruction(0x12 /* ldc */, null, numDims);
                        code.StackMap.PushStack(JavaType.IntegerType);

                        code.NewInstruction(0xB6 /* invokevirtual */, CilType.SystemTypeType,
                                            new JavaMethodRef("MakeArrayType",
                                                    CilType.SystemTypeType, JavaType.IntegerType));

                        stackMap.PopStack(CilMain.Where);   // number of dimensions

                        arrayTypeOnStack = true;
                    }

                    else if (numDims > 1)
                        elemType = elemType.AdjustRank(numDims - 1);
                }

                New(elemType, 1, arrayTypeOnStack);
            }
            else
                throw new InvalidProgramException();
        }



        public void New(CilType elemType, int numDims, bool arrayTypeOnStack = false)
        {
            var elemTypeForArray = elemType.IsGenericParameter
                                 ? CilType.From(JavaType.ObjectType) : elemType;
            var arrayType = elemTypeForArray.AdjustRank(numDims);

            if (elemType.IsGenericParameter)
            {
                /*if (numDims != 1)
                    throw new Exception("unsupported number of dimensions in generic array");*/

                if (! arrayTypeOnStack)
                    GenericUtil.LoadMaybeGeneric(elemType, code);

                var parameters = new List<JavaFieldRef>();
                for (int i = 0; i < numDims; i++)
                    parameters.Add(new JavaFieldRef("", JavaType.IntegerType));
                parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));

                code.NewInstruction(0xB8 /* invokestatic */, SystemArrayType,
                                    new JavaMethodRef("New", JavaType.ObjectType, parameters));

                stackMap.PopStack(CilMain.Where);   // type

                while (numDims-- > 0)
                    stackMap.PopStack(CilMain.Where);

                arrayType = GenericArrayType;
            }

            else if (elemType.IsReference || numDims > 1)
            {
                if (numDims == 1)
                    code.NewInstruction(0xBD /* anewarray */, elemType, null);
                else
                    code.NewInstruction(0xC5 /* multianewarray */, arrayType, numDims);

                for (int i = 0; i < numDims; i++)
                    stackMap.PopStack(CilMain.Where);

                stackMap.PushStack(arrayType);  // arrayObj

                if (elemType.IsValueClass)
                {
                    code.NewInstruction(0x59 /* dup */, null, null);
                    stackMap.PushStack(arrayType);      // arrayCopy

                    if (elemType.HasGenericParameters)
                    {
                        // array of a value class ArrayElement<T>, pass this type
                        // as the second parameter to system.Array.Initialize

                        GenericUtil.LoadMaybeGeneric(elemType, code);

                        // third parameter is null
                        code.NewInstruction(0x01 /* aconst_null */, null, null);
                        stackMap.PushStack(JavaType.ObjectType);
                    }
                    else
                    {
                        // array of a plain value class, pass a constructed object
                        // as the third parameter to system.Array.Initialize

                        // second parameter is null
                        code.NewInstruction(0x01 /* aconst_null */, null, null);
                        stackMap.PushStack(CilType.SystemTypeType);

                        // model parameter is a new object
                        code.NewInstruction(0xBB /* new */, elemType.AsWritableClass, null);
                        stackMap.PushStack(elemType);
                        code.NewInstruction(0x59 /* dup */, null, null);
                        stackMap.PushStack(elemType);
                        code.NewInstruction(0xB7 /* invokespecial */, elemType.AsWritableClass,
                                            new CilMethod(elemType));
                        stackMap.PopStack(CilMain.Where);
                    }

                    code.NewInstruction(0x12 /* ldc */, null, numDims);
                    stackMap.PushStack(JavaType.IntegerType);

                    code.NewInstruction(0xB8 /* invokestatic */, SystemArrayType, InitArrayMethod);

                    stackMap.PopStack(CilMain.Where);   // numDims
                    stackMap.PopStack(CilMain.Where);   // elemType
                    stackMap.PopStack(CilMain.Where);   // arrayType
                    stackMap.PopStack(CilMain.Where);   // arrayCopy
                }

                stackMap.PopStack(CilMain.Where);   // arrayObj
            }
            else
            {
                code.NewInstruction(0xBC /* newarray */, null, elemType.NewArrayType);

                while (numDims-- > 0)
                    stackMap.PopStack(CilMain.Where);
            }

            stackMap.PushStack(arrayType);
        }



        public void Load(Code op, object data, Mono.Cecil.Cil.Instruction inst)
        {
            stackMap.PopStack(CilMain.Where);   // pop index
            CilType arrayType = (CilType) stackMap.PopStack(CilMain.Where);
            CilType elemType = null;
            TypeCode elemCode = 0;

            switch (op)
            {
                case Code.Ldelem_Ref:
                    if (arrayType.IsValueClass)
                        throw new InvalidProgramException();
                    elemType = arrayType.AdjustRank(-1);
                    break;

                case Code.Ldelem_Any:

                    //if (data is ArrayType || (! (data is TypeReference)))
                    if (! (data is TypeReference))
                        throw new InvalidProgramException();
                    elemType = CilType.From((TypeReference) data);

                    // check if array element is loaded and boxed, only to check
                    // its type or check if null.  in this case we take a shortcut
                    // and load directly from the array, without making a copy.

                    var next = inst.Next;
                    if (next != null && next.OpCode.Code == Code.Box)
                    {
                        next = next.Next;
                        if (next != null && CodeBuilder.IsBrTrueBrFalseIsInst(next.OpCode.Code))
                        {
                            code.NewInstruction(0xB8 /* invokestatic */,
                                    new JavaType(0, 0, "java.lang.reflect.Array"),
                                    new JavaMethodRef("get", JavaType.ObjectType, JavaType.ObjectType, JavaType.IntegerType));
                            stackMap.PushStack(elemType);
                            return;
                        }
                    }

                    break;

                case Code.Ldelem_I1: case Code.Ldelem_U1:   elemCode = TypeCode.Byte;   break;
                case Code.Ldelem_U2:                        elemCode = TypeCode.Char;   break;
                case Code.Ldelem_I2:                        elemCode = TypeCode.Int16;  break;
                case Code.Ldelem_I4: case Code.Ldelem_U4:   elemCode = TypeCode.Int32;  break;
                case Code.Ldelem_I8: case Code.Ldelem_I:    elemCode = TypeCode.Int64;  break;
                case Code.Ldelem_R4:                        elemCode = TypeCode.Single; break;
                case Code.Ldelem_R8:                        elemCode = TypeCode.Double; break;

                default:                                    throw new InvalidProgramException();
            }

            if (elemCode != 0)
                elemType = CilType.From(new JavaType(elemCode, 0, null));
            Load(arrayType, elemType, inst);
        }



        void Load(CilType arrayType, CilType elemType, Mono.Cecil.Cil.Instruction inst)
        {
            if (arrayType == null)
                arrayType = elemType.AdjustRank(1);
            /*Console.WriteLine("(LOAD) ARRAY TYPE = " + arrayType + "," + arrayType.ArrayRank
                           + " ELEM TYPE = " + elemType + "," + elemType.ArrayRank
                           + " ELEMVAL? " + elemType.IsValueClass
                           + " ELEMGEN? " + elemType.IsGenericParameter);*/

            if (    object.ReferenceEquals(arrayType, GenericArrayType)
                 || elemType.IsGenericParameter || arrayType.IsGenericParameter)
            {
                code.NewInstruction(0xB8 /* invokestatic */,
                                    SystemArrayType, LoadArrayMethod);
                if (elemType.ArrayRank != 0)
                    elemType = GenericArrayType;
            }
            else
            {
                if (    elemType.PrimitiveType == TypeCode.Int16
                     && arrayType.PrimitiveType == TypeCode.Char)
                {
                    // ldelem.i2 with a char[] array, should be 'caload' not 'saload'
                    elemType = arrayType.AdjustRank(-arrayType.ArrayRank);
                }

                code.NewInstruction(elemType.LoadArrayOpcode, null, null);

                if (arrayType.IsValueClass || elemType.IsValueClass)
                    CilMethod.ValueMethod(CilMethod.ValueClone, code);
            }

            stackMap.PushStack(elemType);
        }



        public void Store(Code op)
        {
            TypeCode elemType;

            switch (op)
            {
                case Code.Stelem_Ref: case Code.Stelem_Any:

                    Store(null);
                    return;

                case Code.Stelem_I1:                        elemType = TypeCode.Byte;   break;
                case Code.Stelem_I2:                        elemType = TypeCode.Int16;  break;
                case Code.Stelem_I4:                        elemType = TypeCode.Int32;  break;
                case Code.Stelem_I8: case Code.Stelem_I:    elemType = TypeCode.Int64;  break;
                case Code.Stelem_R4:                        elemType = TypeCode.Single; break;
                case Code.Stelem_R8:                        elemType = TypeCode.Double; break;

                default:                                    throw new InvalidProgramException();
            }

            Store(CilType.From(new JavaType(elemType, 0, null)));
        }



        void Store(CilType elemType)
        {
            stackMap.PopStack(CilMain.Where);                               // value
            stackMap.PopStack(CilMain.Where);                               // index
            var arrayType = stackMap.PopStack(CilMain.Where) as CilType;    // array
            var arrayElemType = arrayType.AdjustRank(-arrayType.ArrayRank);
            if (elemType == null)
                elemType = arrayElemType;

            /*Console.WriteLine("(STORE) ARRAY TYPE = " + arrayType + "," + arrayType.ArrayRank
                           + " ELEM TYPE = " + elemType + "," + elemType.ArrayRank
                           + " ELEMVAL? " + elemType.IsValueClass
                           + " ELEMGEN? " + elemType.IsGenericParameter);*/

            if (    object.ReferenceEquals(arrayType, GenericArrayType))
            {
                // stelem.any T into generic array T[]
                code.NewInstruction(0xB8 /* invokestatic */, SystemArrayType, StoreArrayMethod);
            }
            else if (arrayElemType.IsValueClass && elemType.IsValueClass)
            {
                // storing a value type into an array of value types.
                // we use ValueType.ValueCopy to write over the element.

                int localIndex = locals.GetTempIndex(elemType);
                code.NewInstruction(elemType.StoreOpcode, null, localIndex);

                code.NewInstruction(arrayType.LoadArrayOpcode, null, null);

                code.NewInstruction(elemType.LoadOpcode, null, localIndex);
                locals.FreeTempIndex(localIndex);

                // we can pass any type that is not a generic parameter
                GenericUtil.ValueCopy(CilType.SystemTypeType, code, true);
            }
            else if (arrayType.ArrayRank > 1)
            {
                // always 'aastore' if multidimensional array
                code.NewInstruction(arrayType.StoreArrayOpcode, null, null);
            }
            else
            {
                if (    elemType.PrimitiveType == TypeCode.Int16
                     && arrayType.PrimitiveType == TypeCode.Char)
                {
                    // stelem.i2 with a char[] array, should be 'castore' not 'sastore'
                    elemType = arrayType.AdjustRank(-arrayType.ArrayRank);
                }

                if (arrayType.IsValueClass || elemType.IsValueClass)
                {
                    CilMethod.ValueMethod(CilMethod.ValueClone, code);
                }

                code.NewInstruction(elemType.StoreArrayOpcode, null, null);
            }
        }



        public void Address(CilType arrayType)
        {
            stackMap.PopStack(CilMain.Where);       // index

            if (arrayType == null)
                arrayType = (CilType) stackMap.PopStack(CilMain.Where);
            else
                stackMap.PopStack(CilMain.Where);   // array

            var elemType = arrayType.AdjustRank(-arrayType.ArrayRank);

            if (elemType.IsReference)
            {
                if (elemType.IsGenericParameter)
                {
                    // call system.Array.Box(object array, int index)
                    code.NewInstruction(0xB8 /* invokestatic */, SystemArrayType,
                            new JavaMethodRef("Box", CilType.SystemValueType,
                                              JavaType.ObjectType, JavaType.IntegerType));
                }
                else if (elemType.IsValueClass)
                {
                    code.NewInstruction(0x32 /* aaload */, null, null);
                }
                else
                {
                    // call system.Reference.Box(object a, int i)
                    elemType = new BoxedType(elemType, false);
                    code.NewInstruction(0xB8 /* invokestatic */, elemType,
                                        new JavaMethodRef("Box", elemType,
                                              JavaType.ObjectType, JavaType.IntegerType));
                }
                stackMap.PushStack(elemType);
            }
            else
            {
                // call system.(PrimitiveType).Box(primitiveType[] a, int i)
                var typeCode = elemType.PrimitiveType;

                stackMap.PushStack(new BoxedType(elemType, false));

                arrayType = elemType.AdjustRank(1);
                elemType = elemType.AsWritableClass;
                code.NewInstruction(0xB8 /* invokestatic */, elemType,
                        new JavaMethodRef("Box", elemType, arrayType, JavaType.IntegerType));
            }
        }



        public void Call(CilMethod method, Mono.Cecil.Cil.Instruction inst)
        {
            int numDims = method.Parameters.Count;
            var elemType = method.DeclType;

            if (method.Name == "Get")
            {
                Deref(numDims);

                stackMap.PopStack(CilMain.Where);   // pop index
                stackMap.PopStack(CilMain.Where);   // pop array
                Load(null, elemType, inst);
            }

            else if (method.Name == "Set")
            {
                numDims--;          // last parameter is value
                if (numDims > 1)
                {
                    var valueType = stackMap.PopStack(CilMain.Where);
                    int localIndex = locals.GetTempIndex(valueType);
                    code.NewInstruction(valueType.StoreOpcode, null, localIndex);

                    Deref(numDims);

                    code.NewInstruction(valueType.LoadOpcode, null, localIndex);
                    stackMap.PushStack(valueType);
                }

                Store(elemType);
            }

            else if (method.Name == "Address")
            {
                Deref(numDims);

                Address(elemType);
            }

            else
                throw new InvalidProgramException();
        }



        void Deref(int numDims)
        {
            if (numDims > 1)
            {
                for (int i = 0; i < numDims; i++)
                    stackMap.PopStack(CilMain.Where); // index
                var arrayType = (CilType) stackMap.PopStack(CilMain.Where);

                if (arrayType.ArrayRank == 0)
                {
                    Deref(numDims, true);
                    arrayType = GenericArrayType;
                }
                else
                {
                    Deref(numDims, false);
                    arrayType = arrayType.AdjustRank(-numDims + 1);
                }

                // after recursive de-ref, push a 1-dim array, and index
                stackMap.PushStack(arrayType);
                stackMap.PushStack(JavaType.IntegerType);
            }
        }



        void Deref(int numDims, bool isGenericArray)
        {
            int localIndex = locals.GetTempIndex(JavaType.IntegerType);
            code.NewInstruction(JavaType.IntegerType.StoreOpcode, null, localIndex);

            if (numDims > 2)
                Deref(numDims - 1, isGenericArray);

            if (isGenericArray)
            {
                code.NewInstruction(0xB8 /* invokestatic */,
                                    SystemArrayType, LoadArrayMethod);
            }
            else
            {
                code.NewInstruction(0x32 /* aaload */, null, null);
            }

            code.NewInstruction(JavaType.IntegerType.LoadOpcode, null, localIndex);
            locals.FreeTempIndex(localIndex);
        }



        public static bool MaybeGetProxy(CilType fromType, CilType intoType,
                                         JavaCode code, bool pushFromType = false)
        {
            if (    fromType.IsArray
                 || object.ReferenceEquals(fromType, GenericArrayType)
                 || fromType.Equals(JavaType.StringType))
            {
                if (GenericUtil.ShouldCallGenericCast(fromType, intoType))
                {
                    code.NewInstruction(0xB8 /* invokestatic */,
                                        SystemArrayType, GetProxyMethod);
                    return true;
                }
            }
            return false;
        }



        public static void InitializeArray(object initialValue, JavaCode code)
        {
            //
            // the input array on the stack should be a primitive array
            //

            var array = code.StackMap.PopStack(CilMain.Where);

            int size = 0;
            switch (array.PrimitiveType)
            {
                case TypeCode.Boolean: case TypeCode.SByte: case TypeCode.Byte:   size = 1; break;
                case TypeCode.Char:    case TypeCode.Int16: case TypeCode.UInt16: size = 2; break;
                case TypeCode.Single:  case TypeCode.Int32: case TypeCode.UInt32: size = 4; break;
                case TypeCode.Double:  case TypeCode.Int64: case TypeCode.UInt64: size = 8; break;
                default:                                                          size = 0; break;
            }
            if (size == 0 || array.ArrayRank == 0)
                throw new Exception("invalid array for InitializeArray");

            //
            // the initializer byte buffer should be divisible by element size
            //

            var bytes = initialValue as byte[];
            int count = 0;
            if (bytes != null && bytes.Length != 0 && (bytes.Length % size) == 0)
                count = bytes.Length / size;
            if (count == 0)
                throw new Exception("invalid data for InitializeArray");

            //
            // prepare the operand stack for as many operands as we will need
            //

            code.StackMap.PushStack(array);
            code.StackMap.PushStack(JavaType.IntegerType);
            code.StackMap.PushStack(size == 8 ? JavaType.LongType : JavaType.IntegerType);

            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);
            code.StackMap.PopStack(CilMain.Where);

            //
            // generate a sequence of instructions to initialize the array,
            // a reference to which was already pushed at the top of the stack
            //

            byte storeArrayOpcode =
                    ((CilType) array).AdjustRank(-array.ArrayRank).StoreArrayOpcode;

            for (int index = 0; index < count; index++)
            {
                if (index > 0)
                    code.NewInstruction(0x59 /* dup */, null, null);
                code.NewInstruction(0x12 /* ldc */, null, (int) index);
                code.NewInstruction(0x12 /* ldc */, null,
                                    Value(bytes, index * size, array.PrimitiveType));
                code.NewInstruction(storeArrayOpcode, null, null);
            }

            object Value(byte[] bytes, int offset, TypeCode primitive)
            {
                switch (primitive)
                {
                    case TypeCode.Boolean:
                        bool boolValue = BitConverter.ToBoolean(bytes, offset);
                        return (int) (boolValue ? 1 : 0);
                    case TypeCode.SByte:
                        return (int) (((sbyte[]) (object) bytes)[offset]);
                    case TypeCode.Byte:
                        return (int) bytes[offset];
                    case TypeCode.Char:
                        return (int) BitConverter.ToChar(bytes, offset);
                    case TypeCode.Int16:
                        return (int) BitConverter.ToInt16(bytes, offset);
                    case TypeCode.UInt16:
                        return (int) BitConverter.ToUInt16(bytes, offset);
                    case TypeCode.Int32:
                        return BitConverter.ToInt32(bytes, offset);
                    case TypeCode.UInt32:
                        return (int) BitConverter.ToUInt32(bytes, offset);
                    case TypeCode.Int64:
                        return BitConverter.ToInt64(bytes, offset);
                    case TypeCode.UInt64:
                        return (long) BitConverter.ToUInt64(bytes, offset);
                    case TypeCode.Single:
                        return BitConverter.ToSingle(bytes, offset);
                    case TypeCode.Double:
                        return BitConverter.ToDouble(bytes, offset);
                    default:
                        return null;
                }
            }
        }



        public static bool CheckCast(CilType castType, bool @throw, JavaCode code)
        {
            if (    object.ReferenceEquals(castType, GenericArrayType)
                 || (castType.IsArray && (     castType.IsInterface
                                           ||  castType.IsGenericParameter
                                           ||  castType.ClassName == JavaType.ObjectType.ClassName
                                           ||  castType.ClassName == CilType.SystemValueType.ClassName)))
            {
                // if casting to Object[], ValueType[], to an array of interface type,
                // or to an array of a generic parameter, we can't rely on a simple
                // 'checkcast' or 'instanceof', because the jvm will permit the cast
                // of a value type array to the aforementioned reference types.
                //
                // instead, we generate a call to system.Array.CheckCast in baselib,
                // except if we are generating code for the system.Array class itself.

                if (code.Method.Class.Name.StartsWith("system.Array"))
                    return false;

                // note the caller of this method already popped the stack once.
                // which we have to undo that, before we push anything else.
                code.StackMap.PushStack(JavaType.ObjectType);   // array

                var method = new JavaMethodRef("CheckCast", JavaType.ObjectType);
                method.Parameters.Add(new JavaFieldRef("", JavaType.ObjectType));

                if (    object.ReferenceEquals(castType, GenericArrayType)
                     || castType.IsGenericParameter)
                {
                    method.Parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
                    GenericUtil.LoadMaybeGeneric(castType, code);
                    // CilType.SystemTypeType pushed to the stack
                }
                else
                {
                    method.Parameters.Add(new JavaFieldRef("", JavaType.ClassType));
                    code.NewInstruction(0x12 /* ldc */, castType.AdjustRank(-1), null);
                    code.StackMap.PushStack(JavaType.ClassType);
                }

                method.Parameters.Add(new JavaFieldRef("", JavaType.BooleanType));
                code.NewInstruction(0x12 /* ldc */, null, (int) (@throw ? 1 : 0));
                code.StackMap.PushStack(JavaType.IntegerType);  // boolean

                code.NewInstruction(0xB8 /* invokestatic */, SystemArrayType, method);

                code.StackMap.PopStack(CilMain.Where);  // boolean
                code.StackMap.PopStack(CilMain.Where);  // class/type
                code.StackMap.PopStack(CilMain.Where);  // array

                return true;
            }

            return false;
        }



        static CodeArrays()
        {
            var parameters = new List<JavaFieldRef>(4);
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            parameters.Add(new JavaFieldRef("", CilType.SystemTypeType));
            parameters.Add(new JavaFieldRef("", CilType.SystemValueType));
            parameters.Add(new JavaFieldRef("", JavaType.IntegerType));
            InitArrayMethod = new JavaMethodRef("Initialize", JavaType.VoidType, parameters);

            parameters = new List<JavaFieldRef>(3);
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            parameters.Add(new JavaFieldRef("", JavaType.IntegerType));
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            StoreArrayMethod = new JavaMethodRef("Store", JavaType.VoidType, parameters);

            parameters = new List<JavaFieldRef>(2);
            parameters.Add(new JavaFieldRef("", JavaType.ObjectType));
            parameters.Add(new JavaFieldRef("", JavaType.IntegerType));
            LoadArrayMethod = new JavaMethodRef("Load", JavaType.ObjectType, parameters);
        }



        internal static readonly JavaMethodRef InitArrayMethod;
        internal static readonly JavaMethodRef StoreArrayMethod;
        internal static readonly JavaMethodRef LoadArrayMethod;

        internal static readonly CilType SystemArrayType =
                                        CilType.From(new JavaType(0, 0, "system.Array"));

        internal static readonly JavaMethodRef GetProxyMethod =
                            new JavaMethodRef("GetProxy", SystemArrayType, JavaType.ObjectType);

        // the following type is a plain instance of java.lang.Object, but it is compared
        // by reference in several places, to identify a generic array, i.e. T[].
        internal static readonly CilType GenericArrayType = CilType.From(JavaType.ObjectType);
    }

}

