# Bluebonnet Usage Manual

The command line syntax for Bluebonnet is as follows:

    Bluebonnet input_file [ output_file ] [ :filter ] [ :filter... ]

`input_file` specifies the path to a .NET assembly, a Java class or a Java archive.  If `output_file` is omitted, the program prints a disassembly of the input.  If `output_file` is specified, the program converts the input to the output.

By default, all types/classes in the input are processed; one or more wildcard `:filter` may be specified, to limit processing only to matching types/classes.

#### Java Declarations to .NET Reference Assembly

If `input_file` is a Java archive, then the output written to `output_file` is a [reference assembly](https://docs.microsoft.com/en-us/dotnet/standard/assembly/reference-assemblies) declaring all types in the input.  For example,

    Bluebonnet $JAVA_HOME\jre\lib\rt.jar .obj\Javalib.dll

#### .NET Assembly to Java Code

If `input_file` is a .NET assembly, then the output is a Java archive containing a translation to Java classes and bytecode.

    Bluebonnet .obj\Baselib.dll .obj\Baselib.jar

If the output is an existing archive, it will be updated, not overwritten.  The output can alternatively be
(1) a directory, in which case unzipped classes will be generated in that directory; or (2) a Java `.class` file, if the (possibly filtered) input can translate to a single class.

If the input assembly references other assemblies, they are searched in (1) the directory containing the input assembly; (2) the current directory; (3) the directory containing the Bluebonnet executable (for `Javalib.dll`); (4) each directory listed in the `PATH` environment variable; (5) the directories within `%PROGRAMFILES%\DotNet\Shared\Microsoft.NETCore.App` (with later .NET Core versions taking precedence over earlier ones).

# Java Interoperability

#### Access to a Java class object

.NET C# code which references Java declarations (using a reference assembly, as described above) may use the following syntax to refer to a Java class:

    (java.lang.Class) typeof(sometype)

where `sometype` can be any imported Java class, or non-generic .NET type.

#### Access to a Java class object

Java functional interfaces are supported via an artificial delegate, for example:

    java.lang.Thread.setDefaultUncaughtExceptionHandler(
            ( (java.lang.Thread.UncaughtExceptionHandler.Delegate) (
                    (java.lang.Thread p1, java.lang.Throwable p2) =>
                            { .... }) ).AsInterface() );

In this example, `java.lang.Thread.UncaughtExceptionHandler` is the functional interface, which gets an artificial delegate named `Delegate` as a nested type.  The C# lambda is cast to this delegate, and then the `AsInterface` method is invoked, to convert the delegate to a Java interface.

# Attributes

Bluebonnet recognizes the following attributes:

- `[java.attr.DiscardAttribute]` on a top-level type (class/struct/interface/delegate) to exclude the type from output.  For example, [Baselib/`Object.cs`](https://github.com/spaceflint7/bluebonnet/blob/master/Baselib/src/System/Object.cs) declares a `java.lang.Object` type with a `getClass` method, but there is no need to actually emit a Java class for `java.lang.Object`.  For an example of this outside of Baselib, see [BNA/`Import.cs`](https://github.com/spaceflint7/bna/blob/master/BNA/src/Import.cs).

- `[java.attr.RetainTypeAttribute]` on a field data member indicates not to box the field.  This is useful for fields that participate in a code hot path, as it eliminates double-references when accessing the field.  It should not be used with fields which may be referenced directly from code outside their containing assembly.

- `[java.attr.AsInterfaceAttribute]` on a class causes it to be written in output as an interface.  This allows the creation of default method implementations even with versions of C# that do not support this feature.  See for example [Baselib/`IDisposable.cs`](https://github.com/spaceflint7/bluebonnet/blob/master/Baselib/src/System/IDisposable.cs).

- `[java.attr.RetainNameAttribute]` on a type or method indicates that renaming should be inhibited.  For example, an interface method would be renamed to the concatenation of interface name and method name, to allow a class to implement multiple interfaces.  However, this is not appropriate for implementing a Java interface whose methods already have a pre-set method name.  See for example [Baselib/`IDisposable.cs`](https://github.com/spaceflint7/bluebonnet/blob/master/Baselib/src/System/IDisposable.cs).

These attributes are emitted when Bluebonnet exports Java declarations to a .NET assembly, so they are available when such an assembly is referenced during compilation.

# Performance Considerations

For code that runs in a hot path, consider looking at the generated Java byte code to make sure there are no performance issues.  Things to consider:

- Fields of a reference type are generally held in a double-reference, to permit taking their reference.  If such a field is used in a hot path, consider applying the `RetainType` attribute (see above).

- Boxing a primitive or a reference type (as either a field or an array element) is a relatively expensive operation which allocates a wrapper object.  For example, `a[i] += 2;` generates code that takes the address of an array element.  To generate code which is less expensive when translated to Java, it could be re-written as `var v = a[i] + 2; a[i] = v;`

Note that running Bluebonnet with a single argument that points to a Java archive will print a disassembly of the classes and methods within the archive.

# Inexact Implementation

Here are some known differences, deficiencies and incompatibilities of the Bluebonnet .NET implementation, compared to a proper .NET implementation, in no particular order.

- For desktop applications, the Java entry point `main` (lowercase) must be defined as `public static void main(string[] args)`.

- Namespace names are translated to lowercase package names, as required by Java.  Reflection capitalizes the first letter in each namespace component.

- Value type objects are implemented as normal JVM class objects, and are not copied into the operand stack by value.  Instead, a properly translated method duplicates any non-ref value type arguments that it modifies.

- `Delegate.Method` property is not supported, and throws `MemberAccessException`.

- Reflection information for generic types depends on the existence of the `Signature` attribute in java class files.

- `BeforeFieldInit` is not honored; the static initializer for a class will be called at the discretion of the JVM.  if it is a generic class, the static initializer is called when the generic type is first referenced.

- The type system is weaker than .NET when it comes to generic types, and in some casts and assignments are permitted between generic objects that differ only in their type arguments.

- `ConditionalWeakTable` is an ephemeron table in .NET, where (possibly indirect) references from values to keys in the same table are treated as weak references.  The JVM does not provide such a mechanism.

- Limited pointer indirection is permitted, but pointer arithmetic is prohibited.  `stackalloc` buffers are permitted, but must be allocated and accessed using the same type, and may only be assigned to a `System.Span` of the same type.

- Exceptions originating from Java (JVM or library) are translated to equivalent CLR exception types only when caught.  Additionally, `System.Exception.ToString` prints a stack trace, while `java.lang.Throwable.toString` does not.  This means that uncaught exceptions print the stack twice.

- Rectangular multidimensional arrays [,] are implemented as jagged arrays [][] i.e., like Java arrays.  `typeof(int[,]) == typeof(int[][])`.  `GetArrayRank()` returns the actual rank; in .NET it returns 1 for jagged arrays.  `GetElementType()` returns the basic element; in .NET it returns an array type for jagged arrays.  All arrays implement the generic interfaces; in .NET only single-dimension and jagged arrays.

- Non-zero lower bounds are not supported in `System.Array.CreateInstance`.

- Casting an array object to `System.Array`, or to an interface, will result in a reference to a helper/proxy object which implements this interface.  The program can detect that `object.ReferenceEquals(proxy, array)` is false.  The proxy cannot be cast back to the original array.

- `IEnumerable.Current` implemented for an array of primitive integers always returns signed objects (e.g `System.Int32`), never unsigned (`System.UInt32`).  This does not make a difference in the typical case where the result is used as a primitive value.

- `System.MarshalByRefObject` is translated to `java.lang.Object`.

- Attribute `[MethodImplOptions.Synchronized]` is not supported on constructors.

- Module-level and assembly-level constructors/initializers are not supported.

- `AbandonedMutexException` is not supported.  Any mutex objects still held at time of thread death will never be released.

- The `GetHashCode` implementions attempt to match those in the .NET Framework, which are not necessarily the same as the implementations in .NET Core.

- `StringBuilder` does not support a maximum capacity.

- Standard numeric format strings are supported, but a custom format, or an `IFormatProvider` which also implements `NumberFormatInfo` will cause `ToString` functions to throw an exception.

- Non-ordinal, culture-dependant string comparisons and casing are not 100% same, because of differences between Windows NLS and the `java.text` package.
