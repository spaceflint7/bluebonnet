
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [System.Serializable]
    public abstract class MethodBase : MemberInfo //, System.Runtime.InteropServices._MethodBase
    {

        public abstract object Invoke(object obj, BindingFlags invokeAttr, Binder binder,
                                      object[] parameters, CultureInfo culture);

        public object Invoke(object obj, object[] parameters)
            => Invoke(obj, BindingFlags.Default, null, parameters, null);

        public virtual bool IsGenericMethod => false;
        public virtual bool IsGenericMethodDefinition => false;
        public virtual bool ContainsGenericParameters => false;


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
