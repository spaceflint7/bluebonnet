
using System;
using System.Collections.Generic;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

namespace SpaceFlint.CilToJava
{

    public class CilInterface
    {
        public CilType InterfaceType;
        public List<CilType> GenericTypes;
        public List<CilInterfaceMethod> Methods;
        public JavaCode LoadTypeCode;
        public bool DirectReference;
        public bool SuperImplements;
        public bool Inserted;



        public static List<CilInterface> CollectAll(TypeDefinition fromType)
        {
            var list = new List<CilInterface>();
            if (fromType.HasInterfaces)
                Process(fromType, list, true, false);
            return list;

            void Process(TypeDefinition fromType, List<CilInterface> list,
                         bool directReference, bool markSuperImplements)
            {
                if (fromType.HasInterfaces)
                {
                    foreach (var ifcIterator in fromType.Interfaces)
                    {
                        var fromInterfaceRef = ifcIterator.InterfaceType;
                        var fromInterfaceDef = CilType.AsDefinition(fromInterfaceRef);

                        var genericMark = CilMain.GenericStack.Mark();
                        var interfaceType = CilMain.GenericStack.EnterType(fromInterfaceRef);

                        var myInterface = interfaceType.HasGenericParameters
                                        ? ImportGenericInterface(interfaceType, list)
                                        : ImportPlainInterface(interfaceType, list);

                        if (! myInterface.Inserted)
                        {
                            // interface not inserted yet, which means it was just created

                            myInterface.Methods = CilInterfaceMethod.CollectAll(fromInterfaceDef);
                            myInterface.LoadType(fromInterfaceRef);
                            myInterface.DirectReference = directReference;
                            myInterface.SuperImplements = markSuperImplements;
                            myInterface.Inserted = true;

                            list.Add(myInterface);

                            Process(fromInterfaceDef, list, false, markSuperImplements);
                        }
                        else if (markSuperImplements)
                        {
                            // an interface that was found for the initial type,
                            // was also found in a base type, mark it so

                            myInterface.SuperImplements = true;
                        }

                        CilMain.GenericStack.Release(genericMark);
                    }
                }

                //
                // scan base types to detect interfaces implemented there
                //

                var fromBaseTypeRef = fromType.BaseType;
                if (fromBaseTypeRef != null && ! fromType.IsInterface)
                {
                    var fromBaseTypeDef = CilType.AsDefinition(fromBaseTypeRef);

                    var genericMark = CilMain.GenericStack.Mark();
                    CilMain.GenericStack.EnterType(fromBaseTypeDef);

                    Process(fromBaseTypeDef, list, false, true);

                    CilMain.GenericStack.Release(genericMark);
                }
            }
        }



        static CilInterface ImportPlainInterface(CilType interfaceType, List<CilInterface> list)
        {
            foreach (var ifc in list)
            {
                if (    ifc.InterfaceType.JavaName == interfaceType.JavaName
                     && ifc.GenericTypes == null)
                {
                    return ifc;
                }
            }

            var myInterface = new CilInterface();
            myInterface.InterfaceType = interfaceType;
            return myInterface;
        }



        static CilInterface ImportGenericInterface(CilType interfaceType, List<CilInterface> list)
        {
            var genericTypes = new List<CilType>();
            foreach (var genericParm in interfaceType.GenericParameters)
            {
                var genericMark = CilMain.GenericStack.Mark();
                var (type, index) = CilMain.GenericStack.Resolve(genericParm.JavaName);
                CilMain.GenericStack.Release(genericMark);

                genericTypes.Add(index == 0 ? type : genericParm);
            }

            int n = genericTypes.Count;
            foreach (var ifc in list)
            {
                if (    ifc.InterfaceType.JavaName == interfaceType.JavaName
                     && ifc.GenericTypes != null && ifc.GenericTypes.Count == n)
                {
                    bool allGenericTypesMatch = true;
                    for (int i = 0; i < n; i++)
                    {
                        if (ifc.GenericTypes[i].JavaName != genericTypes[i].JavaName)
                        {
                            allGenericTypesMatch = false;
                            break;
                        }
                    }
                    if (allGenericTypesMatch)
                        return ifc;
                }
            }

            var myInterface = new CilInterface();
            myInterface.InterfaceType = interfaceType;
            myInterface.GenericTypes = genericTypes;
            return myInterface;
        }



        void LoadType(TypeReference fromType)
        {
            var code = new JavaCode();
            code.Method = new JavaMethod();
            code.Instructions = new List<SpaceFlint.JavaBinary.JavaCode.Instruction>();
            code.StackMap = new JavaStackMap();
            GenericUtil.LoadMaybeGeneric(InterfaceType, code);
            code.StackMap.PopStack(CilMain.Where);
            code.MaxStack = code.StackMap.GetMaxStackSize(CilMain.Where);
            LoadTypeCode = code;
        }

    }



    public class CilInterfaceMethod
    {
        public CilMethod Method;
        public string SimpleName;
        public List<CilType> Parameters;
        public CilType ReturnType;



        public CilInterfaceMethod(CilMethod fromMethod)
        {
            var name = fromMethod.Name;
            var idx = name.IndexOf(CilMain.OPEN_PARENS);
            if (idx != -1)
                name = name.Substring(0, idx);
            idx = name.IndexOf("--");
            if (idx != -1)
                name = name.Substring(0, idx);
            idx = name.LastIndexOf('-');
            if (idx != -1)
                name = name.Substring(idx + 1);

            var returnType = (CilType) fromMethod.ReturnType;
            if (returnType.IsGenericParameter)
            {
                var genericMark = CilMain.GenericStack.Mark();
                var (type, index) = CilMain.GenericStack.Resolve(returnType.JavaName);
                if (index == 0)
                    returnType = type;

                CilMain.GenericStack.Release(genericMark);
            }

            int n = fromMethod.Parameters.Count;
            var parameters = new List<CilType>(n);
            for (int i = 0; i < n; i++)
            {
                var parameter = (CilType) fromMethod.Parameters[i].Type;
                if (parameter.IsGenericParameter)
                {
                    parameter = parameter.GenericParameters[0];

                    var genericMark = CilMain.GenericStack.Mark();
                    var (type, index) = CilMain.GenericStack.Resolve(parameter.JavaName);
                    if (index == 0)
                    {
                        if (parameter.ArrayRank != 0)
                            type = type.AdjustRank(parameter.ArrayRank);
                        parameter = type;
                    }

                    CilMain.GenericStack.Release(genericMark);
                }
                parameters.Add(parameter);
            }

            Method = fromMethod;
            SimpleName = name;
            Parameters = parameters;
            ReturnType = returnType;
        }



        public override string ToString()
        {
            var s = ReturnType + " " + SimpleName + "(";
            bool comma = false;
            foreach (var p in Parameters)
            {
                if (comma)
                    s += ",";
                else
                    comma = true;
                s += p.JavaName;
            }
            return s + ")";
        }



        public bool EqualParameters(CilInterfaceMethod other)
        {
            if (! ReturnType.Equals(other.ReturnType))
                return false;
            int n = Parameters.Count;
            if (other.Parameters.Count != n)
                return false;
            for (int i = 0; i < n; i++)
            {
                if (! Parameters[i].Equals(other.Parameters[i]))
                    return false;
            }
            return true;
        }



        public bool PlainCompare(CilInterfaceMethod other)
            => SimpleName == other.SimpleName && EqualParameters(other);



        public bool EqualGenericParameters(CilInterfaceMethod other)
        {
            if (! EqualGenericParameter(ReturnType, other.ReturnType))
                return false;
            int n = Parameters.Count;
            if (other.Parameters.Count != n)
                return false;
            for (int i = 0; i < n; i++)
            {
                if (! EqualGenericParameter(Parameters[i], other.Parameters[i]))
                    return false;
            }
            return true;
        }



        static bool EqualGenericParameter(CilType p1, CilType p2)
        {
            if (p1.IsGenericParameter)
            {
                return (p2.IsGenericParameter);
            }
            if (p1.HasGenericParameters)
            {
                if (p2.HasGenericParameters)
                {
                    int n = p1.GenericParameters.Count;
                    if (n == p2.GenericParameters.Count)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            if (! EqualGenericParameter(p1.GenericParameters[i],
                                                        p2.GenericParameters[i]))
                                return false;
                        }
                        return true;
                    }
                }
                return false;
            }
            return p1.Equals(p2);
        }



        public bool GenericCompare(CilInterfaceMethod other)
            => SimpleName == other.SimpleName && EqualGenericParameters(other);



        public static List<CilInterfaceMethod> CollectAll(TypeDefinition fromType)
        {
            var list = new List<CilInterfaceMethod>();
            Process(fromType, list);
            return list;

            void Process(TypeDefinition fromType, List<CilInterfaceMethod> list)
            {
                foreach (var fromMethod in fromType.Methods)
                {
                    if (      (fromMethod.IsPublic || fromMethod.HasOverrides)
                         && ! (fromMethod.IsStatic || fromMethod.IsConstructor))
                    {
                        if (fromType.IsInterface && fromMethod.HasBody)
                        {
                            // skip default interface methods, they are not needed
                            // in the context of resolving interface implementations
                            continue;
                        }

                        var genericMark = CilMain.GenericStack.Mark();
                        var inputMethod = CilMain.GenericStack.EnterMethod(fromMethod);

                        var outputMethod = new CilInterfaceMethod(inputMethod);

                        bool dup = false;
                        foreach (var oldMethod in list)
                        {
                            if (    oldMethod.Method.Name == outputMethod.Method.Name
                                 && oldMethod.EqualParameters(outputMethod))
                            {
                                dup = true;
                                break;
                            }
                        }

                        if (! dup)
                            list.Add(outputMethod);

                        CilMain.GenericStack.Release(genericMark);
                    }
                }

                var fromBaseTypeRef = fromType.BaseType;
                if (fromBaseTypeRef != null && ! fromType.IsInterface)
                {
                    var fromBaseTypeDef = CilType.AsDefinition(fromBaseTypeRef);

                    var genericMark = CilMain.GenericStack.Mark();
                    CilMain.GenericStack.EnterType(fromBaseTypeDef);

                    Process(fromBaseTypeDef, list);

                    CilMain.GenericStack.Release(genericMark);
                }
            }
        }



        public static CilInterfaceMethod FindMethod(List<CilInterfaceMethod> haystack,
                                                    CilInterfaceMethod needle)
        {
            int n = needle.Parameters.Count;
            foreach (var current in haystack)
            {
                if (current.PlainCompare(needle))
                    return current;
            }
            return null;
        }

    }

}
