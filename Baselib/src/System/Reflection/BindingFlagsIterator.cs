
using System.Reflection;

namespace system.reflection
{

    public sealed class BindingFlagsIterator
    {

        // entrypoint

        public static void Run(BindingFlags bindingAttr,
                               RuntimeType initialType,
                               MemberTypes memberType,
                               System.Predicate<java.lang.reflect.AccessibleObject> callback)
        {
            //
            // calculate modifier AND mask and result for matches
            //

            int modifierMask = 0;
            int modifierValue = 0;

            BindingFlags chk = bindingAttr & (BindingFlags.Public | BindingFlags.NonPublic);
            if (chk != (BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (chk == 0)       // if neither, no methods will match
                    return;
                // methods with internal access are converted to public access,
                // so we cannot honor the distinction between Public and NonPublic
                /*
                modifierMask |= java.lang.reflect.Modifier.PUBLIC;
                if (chk == BindingFlags.Public)
                    modifierValue |= java.lang.reflect.Modifier.PUBLIC;
                */
            }
            bindingAttr &= ~chk;

            chk = bindingAttr & (BindingFlags.Static | BindingFlags.Instance);
            if (chk != (BindingFlags.Static | BindingFlags.Instance))
            {
                if (chk == 0)       // if neither, no methods will match
                    return;
                modifierMask |= java.lang.reflect.Modifier.STATIC;
                if (chk == BindingFlags.Static)
                    modifierValue |= java.lang.reflect.Modifier.STATIC;
            }
            bindingAttr &= ~chk;

            //
            // check if iteration is required on base hierarchy
            //

            RuntimeType stopAtType = null;
            if ((bindingAttr & BindingFlags.DeclaredOnly) != 0)
            {
                stopAtType = initialType;
                bindingAttr &= ~BindingFlags.DeclaredOnly;
            }

            //
            // run iteration
            //

            if (bindingAttr != 0)
            {
                throw new System.PlatformNotSupportedException(
                                    "bad binding flags " + bindingAttr);
            }

            switch (memberType)
            {
                case MemberTypes.Method:
                    RunMethods(modifierMask, modifierValue, initialType, stopAtType, callback);
                    return;

                case MemberTypes.Field:
                    RunFields(modifierMask, modifierValue, initialType, stopAtType, callback);
                    return;
            }

            throw new System.ArgumentException();
        }

        //
        // method iterator
        //

        static void RunMethods(int modifierMask, int modifierValue,
                               RuntimeType initialType, RuntimeType stopAtType,
                               System.Predicate<java.lang.reflect.AccessibleObject> callback)
        {
            var currentType = initialType;
            for (;;)
            {
                #pragma warning disable 0436
                java.lang.reflect.Method[] javaMethods =
                    (java.lang.reflect.Method[]) (object)
                            currentType.JavaClassForArray().getDeclaredMethods();
                #pragma warning restore 0436

                foreach (var javaMethod in javaMethods)
                {
                    int jmodifiers = javaMethod.getModifiers();
                    if ((jmodifiers & modifierMask) == modifierValue)
                    {
                        if (! callback(javaMethod))
                            return;
                    }
                }

                currentType = (system.RuntimeType) currentType.BaseType;
                if (currentType == stopAtType)
                    break;
            }
        }

        //
        // field iterator
        //

        static void RunFields(int modifierMask, int modifierValue,
                              RuntimeType initialType, RuntimeType stopAtType,
                              System.Predicate<java.lang.reflect.AccessibleObject> callback)
        {
            var currentType = initialType;
            for (;;)
            {
                java.lang.reflect.Field[] javaFields =
                    (java.lang.reflect.Field[]) (object)
                            currentType.JavaClassForArray().getDeclaredFields();

                foreach (var javaField in javaFields)
                {
                    int jmodifiers = javaField.getModifiers();
                    if ((jmodifiers & modifierMask) == modifierValue)
                    {
                        if (! callback(javaField))
                            return;
                    }
                }

                currentType = (system.RuntimeType) currentType.BaseType;
                if (currentType == stopAtType)
                    break;
            }
        }

    }

}
