
using System;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{

    public abstract class BaseTest
    {

        //
        // standalone test invoked via command line script "<Solution>/build.sh <TestClass>".
        // compiled first into a standalone assembly <TestClass>.exe, with no references to
        // the MSTest framework, and then translated into a JAR file <TestClass>.jar.
        //
        // but note that during project build, in target BuildSecondaryDll, a secondary DLL
        // is built with defines STANDALONE and JAVAONLY to create a small bootstrapper that
        // provides a Java 'main' method without any references to MSTest.
        //

#if STANDALONE

        public static void Main(string[] args)
        {
#if ! JAVAONLY
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var mainType = System.Type.GetType("Tests." + entryAssembly.GetName().Name);
            var mainObject = (BaseTest) Activator.CreateInstance(mainType);
            mainObject.TestMain();
#endif
        }

        public static void main(string[] args)
        {
            //java.lang.Thread.setDefaultUncaughtExceptionHandler(new UncaughtExceptionHandler());
            if (args.Length < 1)
                Console.WriteLine("missing test class name in JavaTest");
            var mainClass = java.lang.Class.forName("tests." + args[0]);
            var mainObject = (BaseTest) mainClass.newInstance();
            mainObject.TestMain();
        }

        /*public class UncaughtExceptionHandler : java.lang.Thread.UncaughtExceptionHandler
        {
            [java.attr.RetainName] public void uncaughtException(java.lang.Thread t,
                                                                 java.lang.Throwable e)
            {
                Console.WriteLine("Exception in thread " + t + " " + e + "\n" + e.StackTrace);
            }
        }*/

#else

        //
        // full test suite invoked from Visual Studio or from the command line:
        // In Solution directory:
        //      vstest.console.exe --Settings:Tests\Tests.runsettings .obj\tests\Release\Tests.dll
        // In Tests sub directory:
        //      vstest.console.exe --Settings:Tests.runsettings ..\.obj\tests\Release\Tests.dll
        //
        // STANDALONE is not defined, and all tests are built into Tests.dll.
        // note that a secondary DLL Tests2.dll is also built, as described above,
        // but it does not contain any of the code below.
        //

        public TestContext TestContext { get; set; }

        //
        // MSTest entry point for each Test, which runs the tests twice as
        // primary and secondary, then compares the outputs.
        //

        [TestMethod]
        public void Test() => Assert.AreEqual(TestPrimary(), TestSecondary(), false,
                                              "\n\n * * * Failed Test: " + GetType().ToString());

        //
        // Primary test calls the abstract TestMain() which is implemented by
        // each actual test class.  console output is captured for comparison.
        //

        string TestPrimary()
        {
            var writer = new StringWriter();
            var oldOut = Console.Out;
            Console.SetOut(writer);
            TestMain();
            Console.SetOut(oldOut);
            return writer.ToString();
        }

        //
        // Secondary test extracts a specific test class from the full test suite
        // in Test.dll and combines it with the secondary version of BaseTest that
        // was compiled into Test2.dll.  The resulting JAR file is executed and
        // the output is captured for comparison.
        //

        string TestSecondary()
        {
            // check if we have the Java installation directory

            var java = System.Environment.GetEnvironmentVariable("JAVA_HOME");
            Assert.IsNotNull(java, "JAVA_HOME environment variable is not set.");

            // check that we have an active .runsettings file that specifies
            // a TestResults directory below the main solution output directory.
            // note that the project file references a custom .runsettings file,
            // but if invoked from the command line, must be specified explicitly.

            var objDir = Path.GetDirectoryName(TestContext.TestRunDirectory);
            Assert.IsTrue(objDir.ToLowerInvariant().EndsWith("testresults"),
                  "Expected TestRunDirectory to contain 'TestResults' path component, "
                + "but got '" + TestContext.TestRunDirectory + "', check .runsettings file.");
            objDir = Path.GetDirectoryName(objDir);
            Assert.IsTrue(objDir.ToLowerInvariant().EndsWith(".obj"),
                  "Expected TestRunDirectory to contain '.obj' path component, "
                + "but got '" + TestContext.TestRunDirectory + "', check .runsettings file.");

            // make sure we have Tests.dll and Tests2.dll

            var testDll = GetType().Module.FullyQualifiedName;
            Assert.IsTrue(testDll.ToLowerInvariant().EndsWith(".dll"),
                  "Expected module file to end with '.dll', but got '" + testDll + "'.");
            var testJar = testDll + "." + GetType().FullName + ".jar";

            var testDll2 = testDll.Substring(0, testDll.Length - 4) + "2.dll";
            Assert.IsTrue(File.Exists(testDll2), "Failed to locate secondary dll file '" + testDll2 + "'");

            // make sure the namespace of each test is Tests

            Assert.IsTrue(GetType().Namespace == "Tests",
                  "Expected namespace 'Tests', but got '" + GetType().Namespace + "'.");

            // delete the output JAR file and make sure it is not there anymore

            File.Delete(testJar);
            Assert.IsFalse(File.Exists(testJar), "Failed to delete jar file '" + testJar + "'");

            // identify the specific test, which is a class of type this.GetType(),
            // and use our main program to extract it to a JAR file

            var BluebonnetExe = objDir + "/Bluebonnet.exe";
            Assert.IsTrue(File.Exists(BluebonnetExe), "Failed to locate Bluebonnet executable '" + BluebonnetExe + "'");

            var filterCommand = $"\":{GetType().FullName}\"";
            var filterAttribute = Attribute.GetCustomAttribute(
                                        GetType(), typeof(FilterAttribute)) as FilterAttribute;
            if (filterAttribute != null)
            {
                foreach (var s in filterAttribute.filter.Split(' '))
                    filterCommand += $" \":{s}\"";
            }

            var output = Run(BluebonnetExe, $"\"{testDll}\" \"{testJar}\" " + filterCommand);
            Assert.IsTrue(File.Exists(testJar), "Failed to create jar file '" + testJar + "'");

            // also extract the secondary BaseTest from Tests2.dll

            output = Run(BluebonnetExe, $"\"{testDll2}\" \"{testJar}\" \":Tests.BaseTest\" \":{GetType().FullName}\"");

            // finally, run the resulting JAR file.  the main class is tests.BaseTest,
            // which was compiled into Tests2.dll during the project build process,
            // and just now merge into the output JAR, by the preceding step.

            output += Run(java + "/bin/java.exe",
                          $"-Xdiag -cp \"{testJar};{objDir}/Baselib.jar\" tests.BaseTest {GetType().Name}");

            return output;

            string Run(string program, string arguments)
            {
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = program;
                p.StartInfo.Arguments = arguments;
                p.Start();
                if (! p.WaitForExit(10 * 1000))
                    p.Kill();
                var output = p.StandardError.ReadToEnd() + p.StandardOutput.ReadToEnd();
                return output;
            }
        }

#endif

        public abstract void TestMain();

    }

    public class FilterAttribute : System.Attribute
    {
        public string filter;
        public FilterAttribute(string _filter) => filter = _filter;
    }

}
