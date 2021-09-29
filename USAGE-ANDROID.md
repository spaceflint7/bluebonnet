
#### Goal

- Set up development of an Android app using a .NET language.

    - Either in Visual Studio or using the command line `dotnet` tool.


- Use Android Studio to build the app.

    - With a Gradle task to build the .NET project.

    - And a Gradle task to convert the .NET code to Java compiled form.


- Most of development should be possible on Windows without requiring an Android device.

    - Platform-specific parts can be qualified with preprocessor constants.

#### Environment Variables

- Set the environment variable `MSBUILD_EXE` to point to `MSBuild.exe` program file.
    - For example, `C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe`


- Set the environment variable `BLUEBONNET_DIR` to point to a directory containing `Bluebonnet.exe`, `Baselib.jar` and `Android.dll`.

- These files can be downloaded from the [Bluebonnet releases](https://github.com/spaceflint7/bluebonnet/releases) page.

#### .NET Project Using DotNet Tool

- Create a new directory for the project, and change to this directory.

- Create a .NET Core project called `DotNet`:

    - `dotnet new console -n DotNet`

    - You may use some other project type or language instead of a C# console app.

    - The project should be named `DotNet`.


- Edit `DotNet.csproj` to add a reference to Android DLL created by Bluebonnet:

        <ItemGroup>
          <Reference Include="Android">
            <HintPath>$(BLUEBONNET_DIR)\Android.dll</HintPath>
          </Reference>
        </ItemGroup>

- Or, you can create the project using Visual Studio:

#### .NET Project Using Visual Studio

- Create a new project.

- Select a __C# Console Application__ template (.NET Framework or .NET Core).

- Specify `DotNet` for the project name, and check the box to __Place solution and project in the same directory__.

- Or specify some other project name and clear that checkbox, but make sure:

    - The solution name is `DotNet`.

    - In __Project Settings__ in Visual Studio, set the __Assembly name__ to `DotNet`.


- Complete the creation of the project and solution.

- Add a reference to `Android.dll` using Visual Studio, or by editing the project file as shown above.

#### Activity Class

- Leave the `Program.cs` file as it is.

    - Otherwise you may get a compilation error about a missing `Main` method.

    - The class in this file will be discarded from the output, unless explicitly referenced.


- Create a new file named `MainActivity.cs` and paste the following into it:

        #if ANDROID

        namespace com.whatever.example
        {
            public sealed class MainActivity : android.app.Activity
            {
                protected override void onCreate (android.os.Bundle savedInstanceState)
                {
                    android.util.Log.i("EXAMPLE", ">>>>>>>> EXAMPLE ACTIVITY <<<<<<<<");
                    base.onCreate(savedInstanceState);
                    setContentView(R.layout.activity_main);
                }
            }

            #pragma warning disable IDE1006 // Must begin with uppercase letter
            #pragma warning disable CA2211 // Non-constant fields should not be visible

            [java.attr.Discard] // discard in output
            public class R
            {
                [java.attr.Discard] // discard in output
                public class layout
                {
                    [java.attr.RetainType] public static int activity_main;
                }
            }
        }

        #endif

#### Android Project in Android Studio

- Create a new project.

- Select the __Empty Activity__ template.

    - This generates activity definitions in the `AndroidManifest.xml` file.


- For this example, set __Package name__ to `com.whatever.example`.

- Set the __Save location__ to the project root directory.
    - Ignore the warning that this directory is not empty.


- Select __Java__ for the language and a reasonable minimum platform API version (e.g. __18__).

- Complete the creation of the project.

- Delete the Java class generated by Android Studio for the main activity.

    - It should be located in the project class `app/java/com.whatever.example/MainActivity`

    - Alternatively, delete the entire directory of Java source files - `app/src/main/java`

#### Gradle Build Script

- Open the __module__-specific build script.

    - In Android Studio, this is the `build.gradle` file annotated with __Module__ (rather than Project).

    - In the directory structure, this is `app/build.gradle` (rather than the top-level file with the same name).

    - Note that the right file begins with a `plugins` section followed by an `android` section.


- Optional:  Between the `android` section, and the `dependencies` section, insert:

        buildDir = "${project.rootDir}/build/${project.name}"

    - This sets the output build directory just below the top-level of the project.

    - Otherwise the default is the `app/build` directory.


- At the top of the `dependencies` section, insert:

        implementation files("$buildDir/dotnet/dotnet.jar")
        implementation files("${System.env.BLUEBONNET_DIR}/baselib.jar")

- After the `dependencies` section, append:

        task buildDotNet {
            doLast {
                delete("${buildDir}/dotnet/dotnet.jar")
                exec {
                    workingDir "${project.rootDir}"
                    commandLine System.env.MSBUILD_EXE ?: 'msbuild.exe',
                            'DotNet', '-r',
                            '-p:OutputType=Library',
                            '-p:Configuration=Release',
                            '-p:DefineConstants=ANDROID',
                            "-p:OutputPath=$buildDir/dotnet",
                            "-p:IntermediateOutputPath=$buildDir/dotnet/intermediate/"
                }
                exec {
                    commandLine "${System.env.BLUEBONNET_DIR}/Bluebonnet.exe",
                            "${buildDir}/dotnet/dotnet.dll",
                            "${buildDir}/dotnet/dotnet.jar"
                }
            }
        }

        preBuild.dependsOn buildDotNet


- Note the use of environment variables in the build script:

    - `MSBUILD_EXE` should specify the path to the `MSBuild.exe` program.

    - `BLUEBONNET_DIR` should specify the directory containing __Bluebonnet__ binaries.

    - These environment variables should be made visible to Android Studio.

#### Gradle Build Script - Sample

- After updating your `app/build.gradle` file, it should look (more or less) like this:

        plugins {
            id 'com.android.application'
        }

        android {
            compileSdkVersion 30

            defaultConfig {
                applicationId "com.whatever.example"
                minSdkVersion 18
                targetSdkVersion 30
                versionCode 1
                versionName "1.0"

                testInstrumentationRunner "androidx.test.runner.AndroidJUnitRunner"
            }

            buildTypes {
                release {
                    minifyEnabled false
                    proguardFiles getDefaultProguardFile('proguard-android-optimize.txt'), 'proguard-rules.pro'
                }
            }
            compileOptions {
                sourceCompatibility JavaVersion.VERSION_1_8
                targetCompatibility JavaVersion.VERSION_1_8
            }
        }

        buildDir = "${project.rootDir}/build/${project.name}"

        dependencies {

            implementation files("$buildDir/dotnet/dotnet.jar")
            implementation files("${System.env.BLUEBONNET_DIR}/baselib.jar")
            implementation 'androidx.appcompat:appcompat:1.3.0'
            implementation 'com.google.android.material:material:1.4.0'
            testImplementation 'junit:junit:4.+'
            androidTestImplementation 'androidx.test.ext:junit:1.1.3'
            androidTestImplementation 'androidx.test.espresso:espresso-core:3.4.0'
        }

        task buildDotNet {
            doLast {
                delete("${buildDir}/dotnet/dotnet.jar")
                exec {
                    workingDir "${project.rootDir}"
                    commandLine System.env.MSBUILD_EXE ?: 'msbuild.exe',
                            'DotNet', '-r',
                            '-p:OutputType=Library',
                            '-p:Configuration=Release',
                            '-p:DefineConstants=ANDROID',
                            "-p:OutputPath=$buildDir/dotnet",
                            "-p:IntermediateOutputPath=$buildDir/dotnet/intermediate/"
                }
                exec {
                    commandLine "${System.env.BLUEBONNET_DIR}/Bluebonnet.exe",
                            "${buildDir}/dotnet/dotnet.dll",
                            "${buildDir}/dotnet/dotnet.jar"
                }
            }
        }

        preBuild.dependsOn buildDotNet

#### ProGuard Rules

- If you wish to minify your release build using Android R8 (ProGuard), enter the following settings into your `app/proguard-rules.pro` file:

        #
        # these rules prevent discarding of generic types
        #
        -keepclassmembers class * implements system.IGenericEntity {
            public static final java.lang.String ?generic?variance;
            public static final *** ?generic?info?class;
            private system.RuntimeType ?generic?type;
            public static final *** ?generic?info?method (...);
            <init>(...);
        }


- Only if you wish to use F#, then enter the settings below as well.  They are needed with C# code.

        #
        # F# printf
        #
        -keepclassmembers class microsoft.fsharp.core.PrintfImpl$ObjectPrinter {
            *** GenericToString? (...);
        }
        -keepclassmembers class microsoft.fsharp.core.PrintfImpl$Specializations* {
            *** * (...);
        }
        -keep class microsoft.fsharp.core.CompilationMappingAttribute { *; }
        -keep class **$Tags { *; }
        -keepclassmembers class * implements java.io.Serializable {
            *** get_* ();
        }
        -keepattributes InnerClasses

#### To Summarize of All of the Above

- In place of Java source files, a new dependency was added on a JAR file - `dotnet.jar`.

- The `buildDotNet` task was defined to perform the following build commands:

    - Run `MSBuild` on the .NET project, in `Release` configuration, with the preprocessor define `ANDROID`

    - Run `Bluebonnet` on the resulting `dotnet.dll` to create `dotnet.jar`



- The `buildDotNet` task was set to execute before the Gradle `preBuild` task.

- ProGuard rules were added to prevent stripping fields and methods used by Bluebonnet to support .NET generic types.

#### Test It

- Compile and run the project in Android Studio.

- If all goes well, the example app should start in the emulator, and display Hello World!

    - This layout comes from the `activity_main.xml` layout file, generated by Android Studio.

    - `onCreate()` in the C# `MainActivity` class calls `setContentView` to inflate this layout.


- The __Logcat__ tab should show the debug log message printed by `MainActivity.onCreate()`