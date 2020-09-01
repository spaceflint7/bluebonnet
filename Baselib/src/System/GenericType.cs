
using System;

namespace system
{

    public static class GenericType
    {


        public static ValueType New(Type fieldType)
        {
            if (! fieldType.IsValueType)
                return system.Reference.Box(null);
            if (((system.RuntimeType) fieldType).CallConstructor(true) is ValueType newObj)
                return newObj;
            throw new TypeAccessException(fieldType.ToString());
        }



        public static void Copy(object fromObj, object toObj)
        {
            if (toObj is system.Reference toRef)
            {
                toRef.Set((fromObj is system.Reference fromRef) ? fromRef.Get() : fromObj);
            }
            else if (fromObj is ValueType fromValue)
            {
                ((ValueMethod) fromValue).CopyTo((ValueType) toObj);
            }
            else if (fromObj is java.lang.Comparable)
            {
                DelegateUtil.CopyBoxed(fromObj, toObj);
            }

        }



        public static object Load(object theObj)
        {
            return (theObj is system.Reference theRef) ? theRef.Get() : theObj;
        }



        public static object Clone(object theObj)
        {
            if (theObj is system.Reference)
                return theObj;
            else if (theObj is ValueType theValue)
                return ((ValueMethod) theValue).Clone();
            else
                return theObj;
        }



        //
        //
        //



        public static object TryCast(object parentObject, System.Type castToType,
                                     java.util.concurrent.atomic.AtomicReferenceArray proxyArray,
                                     int proxyIndex, java.lang.Class proxyClass,
                                     System.Type proxyType)
        {
            // helper method for IGenericObject.TryCast methods, which are created
            // by CilInterfaceBuilder.BuildTryCastMethod.  invoked by TryCast() of
            // a class that implements one or more generic interfaces, and has an
            // array of proxy objects for those interfaces.
            //
            // castToType is the generic type passed to IGenericObject.TryCast.
            // proxyType is the type of the generic interface which corresponds
            // to the proxy object at index proxyIndex of the array proxyArray.
            //
            // if the types match, then the proxy object is created (if necessary),
            // cached in the proxy array for future calls, and returned.
            // if the types do not match, returns null.

            if (! object.ReferenceEquals(castToType, proxyType))
            {
                // a type implementing IA<T1> may be assigned to IA<T2>,
                // depending on variance of generic parameters.
                // note that this is checked by IsAssignableGenericInterface,
                // which does not rely on the java Signature attribute.

                if (! (    proxyType is RuntimeType proxyRuntimeType
                        && proxyRuntimeType.IsCastableToGenericInterface(castToType,
                                (parentObject is system.Array.ProxySyncRoot parentArray)    )))
                {
                    return null;
                }
            }

            var proxyObject = proxyArray.get(proxyIndex);
            if (proxyObject == null)
            {
                var constructor = proxyClass.getDeclaredConstructor(
                        new java.lang.Class[] { (java.lang.Class) typeof(java.lang.Object) });
                proxyObject = constructor.newInstance(new object[1] { parentObject });
                if (! proxyArray.compareAndSet(proxyIndex, null, proxyObject))
                    proxyObject = proxyArray.get(proxyIndex);
            }

            return proxyObject;
        }



        public static object TestCast(object obj, System.Type castToType, bool @throw)
        {
            // used in the implementation of the following instructions:
            // castclass, isinst, unbox, unbox.any

            if (obj is IGenericObject genericObject)
            {
                if (genericObject.TryCast(castToType) != null)
                    return obj;
                if (obj is system.Array.ProxySyncRoot objArray)
                {
                    var proxy = Array.GetProxy(objArray.SyncRoot, castToType, false);
                    if (proxy != null)
                        return proxy;
                }
            }
            else
            {
                var proxy = Array.GetProxy(obj, castToType, false);
                if (proxy != null)
                    return proxy;
            }
            if (@throw)
                ThrowInvalidCastException(obj, castToType);
            return null;
        }



        public static object CallCast(object obj, System.Type castToType)
        {
            // used in the implementation of the callvirt instruction when
            // invoking methods in a generic interface.
            // see also:  CheckAndBoxArguments in CodeCall

            object proxy;
            if (obj is IGenericObject genericObject)
                proxy = genericObject.TryCast(castToType);
            else
                proxy = Array.GetProxy(obj, castToType, true);
            if (proxy == null)
                ThrowInvalidCastException(obj, castToType);
            return proxy;
        }



        public static void ThrowInvalidCastException(object objToCast, System.Type castToType)
        {
            string msg = "Unable to cast object of type '" + objToCast.GetType().Name
                       + "' to type '" + castToType.Name + "'.";
            throw new InvalidCastException(msg);
        }



        [java.attr.RetainType] public static Type IGenericObjectType = typeof(IGenericObject);
    }



    interface IGenericEntity
    {
        // this is a marker interface used by the system.RuntimeType constructor
        // to identify generic types.  this is implemented by interface types;
        // class types implement IGenericObject, which derives from this type.
    }

    interface IGenericObject : IGenericEntity
    {
        // Returns the type of a generic object (i.e. its '-generic-type' field).
        // Implemented in GenericUtil::BuildGenericTypeMethod()
        Type GetType();

        // Checks if the object can be cast to the generic type parameter.
        // For a class generic type, returns a reference to the object itself.
        // For an interface generic type, returns a reference to a proxy object.
        // If not a generic type, or cannot be cast to the type, returns null.
        // Implemented in ...
        object TryCast(Type genericType);
    }

}

