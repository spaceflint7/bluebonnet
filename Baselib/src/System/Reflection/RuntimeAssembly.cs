
using System;
using System.Reflection;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [Serializable]
    public class RuntimeAssembly : System.Reflection.Assembly, ISerializable
    {

        private java.security.CodeSource JavaCodeSource;

        //
        //
        //

        protected RuntimeAssembly(java.security.CodeSource codeSource)
        {
            JavaCodeSource = codeSource;
        }

        //
        //
        //

        public static RuntimeAssembly GetExecutingAssembly(ref system.threading.StackCrawlMark stackMark)
        {
            var stackTrace = (new java.lang.Throwable()).getStackTrace();
            foreach (var stackElem in stackTrace)
            {
                var clsnm = stackElem.getClassName();
                if (! clsnm.StartsWith("system.reflection."))
                    return GetAssemblyForClass(java.lang.Class.forName(clsnm));
            }
            throw new DllNotFoundException();
        }

        static RuntimeAssembly GetAssemblyForClass(java.lang.Class cls)
        {
            var jar = cls?.getProtectionDomain()?.getCodeSource();
            if (jar != null)
            {
                var map = System.Threading.LazyInitializer
                                .EnsureInitialized<java.util.concurrent.ConcurrentHashMap>(
                                        ref _CodeSourceToAssemblyMap);
                var assembly = (RuntimeAssembly) map.get(jar);
                if (assembly == null)
                {
                    var newAssembly = new RuntimeAssembly(jar);
                    assembly = (RuntimeAssembly) map.putIfAbsent(jar, newAssembly) ?? newAssembly;
                }
                return assembly;
            }
            throw new DllNotFoundException();
        }

        private static java.util.concurrent.ConcurrentHashMap _CodeSourceToAssemblyMap;

        //
        //
        //

        public override System.Reflection.AssemblyName GetName(bool copiedName)
        {
            var name = new System.Reflection.AssemblyName(JavaCodeSource.getLocation().getFile());
            name.Version = new Version();
            return name;
        }

        [java.attr.RetainName]
        public static void nInit(System.Reflection.AssemblyName thisAssemblyName,
                                 out RuntimeAssembly assembly,
                                 bool forIntrospection, bool raiseResolveEvent)
        {
            // call-redirected from System.Reflection.AssemblyName
            assembly = null;
        }

        //
        //
        //

        public override object[] GetCustomAttributes(bool inherit)
            => throw new NotImplementedException("Assembly.GetCustomAttributes");

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (object.ReferenceEquals(attributeType,
                                       typeof(System.Reflection.AssemblyProductAttribute)))
            {
                return new System.Attribute [] {
                    new System.Reflection.AssemblyProductAttribute("DefaultProductAttribute") };
            }
            throw new NotImplementedException("Assembly.GetCustomAttributes");
        }

        //
        // ISerializable
        //

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
            => throw new NotImplementedException();

    }

}
