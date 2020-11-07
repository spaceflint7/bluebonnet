
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace system.reflection
{

    [Serializable]
    public class RuntimeAssembly : System.Reflection.Assembly, ISerializable
    {

        //
        // Singleton
        //

        public static RuntimeAssembly CurrentAssembly
            => System.Threading.LazyInitializer.EnsureInitialized<RuntimeAssembly>(ref theInstance);

        private static RuntimeAssembly theInstance;

        private System.Reflection.Module[] theModuleList;

        public RuntimeAssembly()
        {
            var theModule = (System.Reflection.Module) (object) new RuntimeModule();
            theModuleList = new System.Reflection.Module[] { theModule };
        }

        //
        // GetExecutingAssembly
        //

        public static RuntimeAssembly GetExecutingAssembly(
                                            ref system.threading.StackCrawlMark stackMark)
            => CurrentAssembly;

        //
        // GetModules
        //

        public override System.Reflection.Module[] GetModules(bool getResourceModules)
            => CurrentAssembly.theModuleList;

        //
        // GetName
        //

        public override System.Reflection.AssemblyName GetName(bool copiedName)
        {
            var name = new System.Reflection.AssemblyName("TheAssembly");
            name.CultureName = "neutral";
            name.Version = new Version(0, 0, 0, 0);
            return name;
        }

        public override string FullName => GetName(false).ToString();

        // nToString is called by AssemblyName.FullName
        public static string nToString(System.Reflection.AssemblyName name)
            => $"{name.Name}, Version={name.Version}, Culture={name.CultureName}, PublicKeyToken=0000000000000000";

        //
        // GetManifestResourceStream
        //

        public override System.IO.Stream GetManifestResourceStream(string name) => null;

        public override System.IO.Stream GetManifestResourceStream(System.Type type, string name) => null;

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
