
using System.Runtime.InteropServices;

namespace system.reflection
{

    public abstract class Module
    {
        public abstract System.Type[] GetTypes();
    }

    public sealed class RuntimeModule : Module
    {
        [java.attr.RetainType] public java.security.ProtectionDomain JavaDomain;

        public override System.Type[] GetTypes() => new System.Type[0];

        public MetadataImport MetadataImport => _MetadataImport;
        public StructLayoutAttribute StructLayoutAttribute => _StructLayoutAttribute;

        [java.attr.RetainType] public static MetadataImport _MetadataImport = new MetadataImport();

        [java.attr.RetainType] public static StructLayoutAttribute _StructLayoutAttribute =
                                                        new StructLayoutAttribute(LayoutKind.Auto);
    }

    public struct MetadataImport
    {
        // called by internal method GetCustomAttribute(RuntimeType)
        // in System.Runtime.InteropServices.StructLayoutAttribute
        public void GetClassLayout(int typeTokenDef, out int packSize, out int classSize)
        {
            packSize = 8;   // 0 not allowed? 8 is default?
            classSize = 0;
        }
    }

}
