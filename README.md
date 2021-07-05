# Bluebonnet

This is a partial implementation of the .NET platform on top of the Java Virtual Machine, and compatible with Android runtime.  The **Bluebonnet** bytecode compiler translates .NET [CIL](https://en.wikipedia.org/wiki/Common_Intermediate_Language) into [Java bytecode](https://en.wikipedia.org/wiki/Java_bytecode) in Java classes, and additional run-time support is provided by the **Baselib** library.

https://www.spaceflint.com/bluebonnet

## Highlights

- 100% Java 8 bytecode with no native code.
- Compatible with all Android API levels through [desugaring](https://developer.android.com/studio/write/java8-support).
- Runtime library translated from the .NET Base Class Library.
- Simple interoperability with Java APIs.
- Tested with C# and F# programs.

## Requirements for Building

- Java 8 is required during building, to import Java classes from the `rt.jar` file.
    - Importing from Java 9 modules is not yet supported.
    - The translated code can run on Java 8 or later version.
    - Alternatively, `(ANDROID_HOME)/platforms/android-XX/android.jar` from Android SDK can be copied as `(JAVA_HOME)\jre\lib\rt.jar` file.
- Android build tools with support for `D8`/`R8` desugaring.
    - Tested with build tools version 30.0.2 and platform API version 30.
- .NET Framework (4.7 or later)
    - Tested only on Windows at this time.  May not necessarily build with .NET Core.

[Recent releases](https://github.com/spaceflint7/bluebonnet/releases) include a pre-built `Android.dll` reference assembly, created from the latest Android SDK, as well as all binaries needed for running Bluebonnet.  See the Usage section below.

## Building

- Set `JAVA_HOME` environment variable to the Java 8 home directory.
- Open `Bluebonnet.sln` in Visual Studio 2017 or later, and build in Release configuration.
- Building from the command line is also possible:
    - Open a Visual Studio Developer command prompt.
    - Change to the solution directory.
    - Restore packages using [nuget](https://www.nuget.org/downloads): `nuget restore`
    - Build the project: `msbuild -p:Configuration=Release`

- This should produce `Bluebonnet.exe` and `Baselib.jar` in the `.obj` sub-directory of the solution directory.
    - `Bluebonnet.exe` translates compiled .NET assemblies into Java classes.
    - `Baselib.jar` is required on the classpath when running the generated Java classes.

## Demo

Building a simple Hello World example.  The sample `HelloWorld.cs` in the `Demos` sub-directory uses parallel LINQ, so it exercises generics, delegates, and threads, among many other .NET features.

- Open a Visual Studio Developer command prompt.
- Make sure the `JAVA_HOME` environment variable is set properly.
- Change to the solution directory.
- Compile the demo: `csc -out:.obj\HelloWorld.exe Demos\HelloWorld.cs`
- Test the demo: `.obj\HelloWorld.exe`
    - It should print `Hello, World!`, one character at a time.
- Translate the demo: `.obj\Bluebonnet .obj\HelloWorld.exe .obj\HelloWorld.jar`
- Run the translation: `"%JAVA_HOME%\bin\java" -classpath ".obj\HelloWorld.jar;.obj\Baselib.jar" spaceflint.demos.HelloWorld`

Note that Java package names are lowercase, so .NET namespaces are translated to lowercase, e.g. `SpaceFlint.Demos -> spaceflint.demos`.

There are some additional demos:

- Change to the `Demos` directory inside the solution directory.
- Restore packages using [nuget](https://www.nuget.org/downloads): `nuget restore`
- Build and run each demo:  `msbuild -p:Configuration=Release -t:RunDemo`
    - Note that the Android demos require the `ANDROID_HOME` environment directory, and the project is hard-coded to use Android platform version 28, and build-tools 30.0.2
    - Note also that the Android demos build an APK file, but do not install it.

See the [BNA](https://github.com/spaceflint7/bna) and [Unjum](https://github.com/spaceflint7/unjum) repositories for more demos for Android.

## Usage

For more information about using Bluebonnet, please see the [USAGE.md](USAGE.md) file.  That document also records any known differences and deficiencies, compared to a proper .NET implementation.  To use Bluebonnet in Android Studio, see [USAGE-ANDROID.md](USAGE-ANDROID.md).