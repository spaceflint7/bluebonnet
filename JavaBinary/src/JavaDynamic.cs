
using System;
using System.Collections.Generic;

namespace SpaceFlint.JavaBinary
{

    public class JavaCallSite
    {
        public JavaMethodHandle BootstrapMethod;
        public object[] BootstrapArgs;
        public JavaMethodRef InvokedMethod;
        public ushort BootstrapMethodIndex;



        public JavaCallSite(ushort index, JavaMethodRef method)
        {
            InvokedMethod = method;
            BootstrapMethodIndex = index;
        }



        //
        // create a CallSite that returns an object that implements the
        // functional interface 'declType', containing a single method with the
        // signature 'declMethod'.  this interface method is a proxy that calls
        // the method 'implMethod' from class 'implClass'.
        //
        // 'captureParameters' may optionally specify any number of parameters,
        // which would be on the stack when 'invokedynamic' is executed.  these
        // parameters are captured by the callsite, and injected as the first
        // several parameters of 'implMethod' when the proxy is called.  the
        // parameter list for 'implMethod' should not include these parameters;
        // they are inserted by this method.
        //
        // callKind should be InvokeStatic, InvokeVirtual, or InvokeInterface,
        // and specifies the type of 'implMethod'.  if not InvokeStatic, it is
        // as if the first parameter in 'captureParameters' is 'declType'.
        //

        public JavaCallSite(JavaType declType, JavaMethodRef declMethod,
                            JavaType implClass, JavaMethodRef implMethod,
                            List<JavaFieldRef> captureParameters,
                            JavaMethodHandle.HandleKind callKind)
        {
            BootstrapMethod = GetLambdaMetafactory();
            BootstrapMethodIndex = 0xFFFF;

            var invokedMethod = new JavaMethodRef();
            invokedMethod.Name = declMethod.Name;
            invokedMethod.ReturnType = declType;
            invokedMethod.Parameters = new List<JavaFieldRef>();
            if (callKind != JavaMethodHandle.HandleKind.InvokeStatic)
                invokedMethod.Parameters.Add(new JavaFieldRef("", implClass));
            InvokedMethod = invokedMethod;

            var methodHandle = new JavaMethodHandle();
            methodHandle.Class = implClass;
            methodHandle.Method = implMethod;
            methodHandle.Kind = callKind;
            methodHandle.IsInterfaceMethod =
                                (callKind == JavaMethodHandle.HandleKind.InvokeInterface);

            JavaMethodType castMethod = new JavaMethodType(declMethod.ReturnType, null);
            castMethod.Parameters = new List<JavaFieldRef>();
            for (int i = 0; i < implMethod.Parameters.Count; i++)
            {
                JavaType castParameterType = declMethod.Parameters[i].Type;
                if (castParameterType.Equals(JavaType.ObjectType))
                {
                    var implParameterType = implMethod.Parameters[i].Type;
                    castParameterType = implParameterType.Wrapper ?? implParameterType;
                }
                castMethod.Parameters.Add(new JavaFieldRef("", castParameterType));
            }

            BootstrapArgs = new object[3];
            // arg#0 (aka 'samMethodType') specifies the signature, minus the name,
            // of the method in the functional interface.  note that the name was
            // already inserted into 'InvokedMethod', above
            BootstrapArgs[0] =
                new JavaMethodType(declMethod.ReturnType, declMethod.Parameters);
            // arg#1 (aka 'implMethod') specifies the name and signature of the
            // method that provides the actual implementation for the interface
            BootstrapArgs[1] = methodHandle;
            // arg#2 (aka 'instantiatedMethodType') specifies optional conversions
            // on the parameters passed to the 'invokeinterface' instructions.
            // in particular, boxing of primitives to standard wrapper types, or
            // casting generic java.lang.Object to a more restricted type.
            BootstrapArgs[2] =
                new JavaMethodType(castMethod.ReturnType, castMethod.Parameters);

            if (captureParameters != null)
            {
                invokedMethod.Parameters.AddRange(captureParameters);
                var newParameters = new List<JavaFieldRef>(captureParameters);
                newParameters.AddRange(implMethod.Parameters);
                methodHandle.Method = new JavaMethodRef(
                        implMethod.Name, implMethod.ReturnType, newParameters);
            }
        }



        static JavaMethodHandle GetLambdaMetafactory()
        {
            if (_LambdaMetafactory == null)
            {
                var lookupArg = new JavaFieldRef("",
                                        new JavaType(0, 0, "java.lang.invoke.MethodHandles$Lookup"));
                var stringArg = new JavaFieldRef("", JavaType.StringType);
                var methodTypeArg = new JavaFieldRef("",
                                        new JavaType(0, 0, "java.lang.invoke.MethodType"));
                var methodHandleArg = new JavaFieldRef("",
                                        new JavaType(0, 0, "java.lang.invoke.MethodHandle"));

                var parameters = new List<JavaFieldRef>();
                parameters.Add(lookupArg);
                parameters.Add(stringArg);
                parameters.Add(methodTypeArg);
                parameters.Add(methodTypeArg);
                parameters.Add(methodHandleArg);
                parameters.Add(methodTypeArg);

                var factory = new JavaMethodHandle();
                factory.Kind = JavaMethodHandle.HandleKind.InvokeStatic;
                factory.Class = new JavaType(0, 0, "java.lang.invoke.LambdaMetafactory");
                factory.Method = new JavaMethodRef("metafactory",
                                                   new JavaType(0, 0, "java.lang.invoke.CallSite"),
                                                   parameters);

                _LambdaMetafactory = factory;
            }

            return _LambdaMetafactory;
        }



        static JavaMethodHandle _LambdaMetafactory;
    }



    public class JavaMethodHandle
    {

        public enum HandleKind
        {
            Unused,
            GetField,
            GetStatic,
            PutField,
            PutStatic,
            InvokeVirtual,
            InvokeStatic,
            InvokeSpecial,
            NewInvokeSpecial,
            InvokeInterface
        }



        public HandleKind Kind;
        public JavaType Class;
        public JavaFieldRef Field;
        public JavaMethodRef Method;
        public bool IsInterfaceMethod;



        bool HasFieldRef => Kind <= HandleKind.PutStatic;
        bool HasMethodRef => Kind >= HandleKind.InvokeVirtual;
        //bool HasInterfaceRef => Kind == HandleKind.InvokeInterface;



        public JavaMethodHandle()
        {
        }



        public JavaMethodHandle(JavaReader rdr, byte referenceKind, ushort referenceIndex)
        {
            Kind = (HandleKind) referenceKind;

            var referenceType = rdr.ConstType(referenceIndex);
            bool ok = false;

            if (Kind <= HandleKind.PutStatic)
            {
                if (referenceType == typeof(JavaConstant.FieldRef))
                {
                    (Class, Field) = rdr.ConstField(referenceIndex);
                    ok = true;
                }
            }
            else if (Kind <= HandleKind.NewInvokeSpecial)
            {
                if (referenceType == typeof(JavaConstant.MethodRef))
                {
                    (Class, Method) = rdr.ConstMethod(referenceIndex);
                    ok = true;
                }
                else if (referenceType == typeof(JavaConstant.InterfaceMethodRef))
                {
                    (Class, Method) = rdr.ConstInterfaceMethod(referenceIndex);
                    IsInterfaceMethod = true;
                    ok = true;
                }
            }
            else if (Kind == HandleKind.InvokeInterface)
            {
                if (referenceType == typeof(JavaConstant.InterfaceMethodRef))
                {
                    (Class, Method) = rdr.ConstInterfaceMethod(referenceIndex);
                    IsInterfaceMethod = true;
                    ok = true;
                }
            }

            if (! ok)
                throw rdr.Where.Exception("invalid method handle");
        }



        internal JavaConstant.MethodHandle ToConstant(JavaWriter wtr)
        {
            int referenceIndex = -1;

            if (Kind <= HandleKind.PutStatic)
            {
                referenceIndex = wtr.ConstField(Class, Field);
            }
            else if (Kind <= HandleKind.NewInvokeSpecial)
            {
                if (IsInterfaceMethod)
                    referenceIndex = wtr.ConstInterfaceMethod(Class, Method);
                else
                    referenceIndex = wtr.ConstMethod(Class, Method);
            }
            else if (Kind == HandleKind.InvokeInterface && IsInterfaceMethod)
            {
                referenceIndex = wtr.ConstInterfaceMethod(Class, Method);
            }

            if (referenceIndex == -1)
                throw wtr.Where.Exception("invalid method handle");

            return new JavaConstant.MethodHandle((byte) Kind, (ushort) referenceIndex);
        }



        public override string ToString()
        {
            string s = $"({Kind}) ";
            if (HasFieldRef)
                s += $"{Class}.{Field}";
            else
                s += $"{Method.ReturnType} {Class}.{Method.Name}{Method.ParametersToString()}";
            return s;
        }

    }

}
