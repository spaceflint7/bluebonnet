
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public abstract class MethodBase : MemberInfo //, System.Runtime.InteropServices._MethodBase
    {

        //
        //
        //

        public abstract ParameterInfo[] GetParameters();

        public abstract MethodImplAttributes GetMethodImplementationFlags();

        public abstract System.RuntimeMethodHandle MethodHandle { get; }

        public abstract MethodAttributes Attributes { get; }

        public abstract object Invoke(object obj, BindingFlags invokeAttr, Binder binder,
                                      object[] parameters, CultureInfo culture);

        //
        //
        //

        public virtual MethodImplAttributes MethodImplementationFlags
            => GetMethodImplementationFlags();

        public object Invoke(object obj, object[] parameters)
            => Invoke(obj, BindingFlags.Default, null, parameters, null);

        public virtual Type[] GetGenericArguments()
            => throw new System.NotSupportedException();

        public virtual bool IsGenericMethod => false;
        public virtual bool IsGenericMethodDefinition => false;
        public virtual bool ContainsGenericParameters => false;

        //
        //
        //

        public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;

        public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;

        public bool IsFamily => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;

        public bool IsAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;

        public bool IsFamilyAndAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;

        public bool IsFamilyOrAssembly => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;

        public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;

        public bool IsFinal => (Attributes & MethodAttributes.Final) != 0;

        public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;

        public bool IsHideBySig => (Attributes & MethodAttributes.HideBySig) != 0;

        public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;

        public bool IsSpecialName => (Attributes & MethodAttributes.SpecialName) != 0;

        /*
        // _MethodBase

        void System.Runtime.InteropServices._MethodBase.GetTypeInfoCount(out uint pcTInfo)
            => throw new NotImplementedException();

        void System.Runtime.InteropServices._MethodBase.GetTypeInfo(uint iTInfo, uint lcid, System.IntPtr ppTInfo)
            => throw new NotImplementedException();

        void System.Runtime.InteropServices._MethodBase.GetIDsOfNames(ref System.Guid riid, System.IntPtr rgszNames, uint cNames, uint lcid, System.IntPtr rgDispId)
            => throw new NotImplementedException();

        void System.Runtime.InteropServices._MethodBase.Invoke(uint dispIdMember, ref System.Guid riid, uint lcid, short wFlags, System.IntPtr pDispParams, System.IntPtr pVarResult, System.IntPtr pExcepInfo, System.IntPtr puArgErr)
            => throw new NotImplementedException();
        */
    }
}
