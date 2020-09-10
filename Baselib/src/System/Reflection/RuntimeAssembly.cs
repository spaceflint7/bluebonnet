
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [Serializable]
    public class RuntimeAssembly : System.Reflection.Assembly, ISerializable
    {

        [java.attr.RetainType] private java.security.ProtectionDomain JavaDomain;
        private System.Reflection.Module[] theModule;
        private static java.util.concurrent.ConcurrentHashMap _DomainToAssemblyMap;

        //
        //
        //

        private RuntimeAssembly(java.security.ProtectionDomain domain)
        {
            JavaDomain = domain;
        }

        //
        // GetAssemblyForDomain
        //

        public static System.Reflection.Assembly GetAssemblyForDomain(
                                                java.security.ProtectionDomain domain)
        {
            if (domain == null)
                throw new DllNotFoundException();
            var map = System.Threading.LazyInitializer
                            .EnsureInitialized<java.util.concurrent.ConcurrentHashMap>(
                                    ref _DomainToAssemblyMap);
            var assembly = (RuntimeAssembly) map.get(domain);
            if (assembly == null)
            {
                var newAssembly = new RuntimeAssembly(domain);
                assembly = (RuntimeAssembly)
                                map.putIfAbsent(domain, newAssembly) ?? newAssembly;
            }
            return assembly;
        }

        //
        // GetExecutingAssembly
        //

        public static RuntimeAssembly GetExecutingAssembly(
                                            ref system.threading.StackCrawlMark stackMark)
        {
            java.security.ProtectionDomain domain = null;
            var stackTrace = (new java.lang.Throwable()).getStackTrace();
            foreach (var stackElem in stackTrace)
            {
                var clsnm = stackElem.getClassName();
                if (! clsnm.StartsWith("system.reflection."))
                {
                    domain = java.lang.Class.forName(clsnm)?.getProtectionDomain();
                    break;
                }
            }
            return (RuntimeAssembly) GetAssemblyForDomain(domain);
        }

        //
        // GetModules
        //

        public override System.Reflection.Module[] GetModules(bool getResourceModules)
        {
            return System.Threading.LazyInitializer
                    .EnsureInitialized<System.Reflection.Module[]>(ref theModule, () =>
            {
                var mod = (System.Reflection.Module) (object)
                                (new RuntimeModule() { JavaDomain = JavaDomain });
                return new System.Reflection.Module[] { mod };
            });
        }

        //
        // GetName
        //

        public override System.Reflection.AssemblyName GetName(bool copiedName)
        {
            var name = new System.Reflection.AssemblyName(
                                JavaDomain.getCodeSource().getLocation().getFile());
            name.Version = new Version();
            return name;
        }

        //
        // nInit
        //

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
