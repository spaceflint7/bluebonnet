
using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mono.Cecil;
using SpaceFlint.JavaBinary;

public class CilTool
{

    static int Main(string[] args)
    {
        var filter = new List<Regex>();
        var numArgs = ParseCommandLine(args, filter);
        if (numArgs <= 0)
            return 1;
        if (filter.Count == 0)
            filter = null;

        try
        {
            using (var file = File.OpenRead(ResolveInputPath(args[0])))
            {
                var (byte0, byte1) = (file.ReadByte(), file.ReadByte());

                if (    (byte0 == 0xCA && byte1 == 0xFE)    // class file
                     || (byte0 == 0x50 && byte1 == 0x4B)    // zip file
                     || (byte0 == 0x4D && byte1 == 0x5A))   // exe file
                {
                    file.Position = 0;

                    var outputPath = (numArgs == 2 ? args[1] : null);

                    if (byte0 == 0x4D)
                    {
                        MainDotNet(file, outputPath, filter);
                    }
                    else
                    {
                        var isArchive = (byte0 == 0x50);
                        MainJava(file, isArchive, outputPath, filter);
                    }
                }
                else
                {
                    Console.WriteLine(
                        $"error: cannot determine type of input file {file.Name}");
                    Console.WriteLine();
                    return 1;
                }
            }
        }
        catch (Exception e)
        {
            //var pgmName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            var pgmName = "Bluebonnet";
            string eName =
                (e is JavaException) ? string.Empty : " (" + e.GetType().Name + ")";
            Console.WriteLine();
            Console.WriteLine(pgmName + " error: " + e.Message + eName);
            Console.WriteLine();
            #if DEBUGDIAG
            Console.WriteLine(e);
            #endif
            return 1;
        }

        return 0;
    }



    static int ParseCommandLine(string[] args, List<Regex> filter)
    {
        int n;
        for (n = args.Length; n > 1; n--)
        {
            var arg = args[n - 1];
            if (arg == null || arg.Length < 2 || arg[0] != ':')
                break;
            var regexString =
                    "^" + Regex.Escape(arg.Substring(1))
                               .Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            filter.Insert(0, new Regex(regexString, RegexOptions.Compiled));
        }
        if (n < 1 || n > 2)
        {
            var name = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
            Console.WriteLine($"usage: {name} inputfile [outputfile] [:filter]");
            Console.WriteLine("If one file is specified, prints contents of Java class/JAR or .Net assembly.");
            Console.WriteLine("If two files are specified, converts Java class/JAR to .Net assembly or vice versa.");
            n = -1;
        }
        return n;
    }



    static void MainDotNet(Stream inputStream, string outputPath,
                           List<Regex> filter)
    {
        ModuleDefinition module = ReadModuleAndSymbols(inputStream);

        var moduleTypes = new List<TypeDefinition>(module.Types);
        if (moduleTypes[0].FullName == "<Module>")
            moduleTypes.RemoveAt(0);

        if (filter != null)
        {
            var n = moduleTypes.Count;
            while (n > 0)
            {
                if (! MatchFilter(moduleTypes[--n].FullName, false, filter))
                    moduleTypes.RemoveAt(n);
            }
        }

        if (outputPath == null)
        {
            PrintAssembly(moduleTypes);
            return;
        }

        var classes = SpaceFlint.CilToJava.CilMain.Import(moduleTypes);
        if (classes.Count == 0)
        {
            Console.WriteLine("warning: there are no types to process");
        }
        else
        {
            MainDotNet2(classes, outputPath);
        }
    }


    static void MainDotNet2(List<JavaClass> classes, string outputPath)
    {
        #if ! DEBUG
        string outputType = "";
        try
        {
        #endif

        if (IsDirectoryPath(outputPath))
        {
            #if ! DEBUG
            outputType = "directory";
            #endif
            WriteClassesToDir(classes, outputPath);
        }
        else if (outputPath.ToLower().EndsWith(".class"))
        {
            #if ! DEBUG
            outputType = "file";
            #endif
            if (classes.Count != 1)
                Console.WriteLine("warning: input contains more than one class");
            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                JavaWriter.WriteClass(classes[0], fileStream);
            }
        }
        else
        {
            #if ! DEBUG
            outputType = "ZIP file";
            #endif
            WriteClassesToZip(classes, outputPath);
        }

        #if ! DEBUG
        }
        catch (Exception e)
        {
            if (! (e is JavaException))
            {
                e = new JavaException(e.Message + " in writing output " + outputType,
                                    new JavaException.Where());
            }
            throw e;
        }
        #endif
    }



    static void MainJava(Stream inputStream, bool isArchive, string outputPath,
                         List<Regex> filter)
    {
        var classes = new List<JavaClass>();
        bool printing = (outputPath == null);

        if (isArchive)
        {
            using (var archive = new ZipArchive(inputStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (filter == null || MatchFilter(entry.FullName, true, filter))
                    {
                        var jclass = JavaReader.ReadClass(entry, printing);
                        if (jclass != null)
                            classes.Add(jclass);
                    }
                }
            }
        }
        else
        {
            classes.Add(JavaReader.ReadClass(inputStream, printing));

            if (filter != null)
                Console.WriteLine("warning: filters are ignored for a .class file");
        }

        if (printing)
        {
            PrintClasses(classes);
        }
        else if (classes.Count == 0)
        {
            Console.WriteLine("warning: there are no classes to process");
        }
        else
        {
            ExportClassesToDll(classes, outputPath);
        }
    }



    static bool IsDirectoryPath(string outputPath)
    {
        if (    outputPath[outputPath.Length - 1] == '/'
             || outputPath[outputPath.Length - 1] == '\\')
        {
            return true;
        }

        try
        {
            if ((File.GetAttributes(outputPath) & FileAttributes.Directory) != 0)
            {
                return true;
            }
        } catch (FileNotFoundException)
        {
        } catch (DirectoryNotFoundException)
        {
        }

        return false;
    }



    static void WriteClassesToDir(List<JavaClass> classes, string outputPath)
    {
        foreach (var jclass in classes)
        {
            var filePath = outputPath + "/" + jclass.FilePath();

            var dirPath = Path.GetDirectoryName(filePath);
            if (dirPath != null)
                Directory.CreateDirectory(dirPath);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                JavaWriter.WriteClass(jclass, fileStream);
            }
        }
    }



    static void WriteClassesToZip(List<JavaClass> classes, string outputPath)
    {
        using (var zipStream = new FileStream(outputPath, FileMode.OpenOrCreate))
        {
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Update))
            {
                foreach (var jclass in classes)
                {
                    var path = jclass.FilePath();

                    var entry = archive.GetEntry(path);
                    if (entry != null)
                        entry.Delete();

                    try
                    {
                        entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            JavaWriter.WriteClass(jclass, entryStream);
                        }
                    }
                    catch (Exception e)
                    {
                        #if DEBUGDIAG
                        Console.WriteLine(e);
                        #endif
                        throw new Exception(e.Message + " in entry '" + path + "'");
                    }
                }
            }
        }
    }



    static void ExportClassesToDll(List<JavaClass> classes, string outputPath)
    {
        ModuleDefinition module;

        bool exists = File.Exists(outputPath);
        if (exists)
        {
            var parameters = new ReaderParameters(ReadingMode.Deferred);
            parameters.ReadWrite = true;
            module = ModuleDefinition.ReadModule(outputPath, parameters);
        }
        else
            module = ModuleDefinition.CreateModule(Path.GetFileName(outputPath), ModuleKind.Dll);
        (new DotNetImporter(module)).Merge(classes);

        if (exists)
            module.Write();
        else
            module.Write(outputPath);
    }



    static void PrintClasses(List<JavaClass> classes)
    {
        var printer = new IndentedText();
        foreach (var jclass in classes)
            jclass.PrintJava(printer);
        Console.WriteLine(printer.ToString());
    }



    static void PrintAssembly(List<TypeDefinition> moduleTypes)
    {
        var printer = new IndentedText();
        foreach (var type in moduleTypes)
            DotNetPrinter.PrintType(printer, type);
        Console.WriteLine(printer.ToString());
    }



    static bool MatchFilter(string name, bool java, List<Regex> filter)
    {
        if (java)
        {
            int last = name.LastIndexOf(".class");
            if (last != -1)
                name = name.Substring(0, last);
            name = name.Replace('/', '.');
        }
        foreach (var f in filter)
        {
            if (f.IsMatch(name))
                return true;
        }
        return false;
    }



    static ModuleDefinition ReadModuleAndSymbols(Stream file)
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        if (    assembly.GetType("Mono.Cecil.Pdb.NativePdbReaderProvider") == null
             || assembly.GetType("Mono.Cecil.Mdb.MdbReaderProvider") == null)
        {
            // if cecil DLLs were merged into our assembly, then the above should work.
            // otherwise, we try to locate the needed types in external DLLs.

            if (Type.GetType("Mono.Cecil.Pdb.NativePdbReaderProvider, Mono.Cecil.Pdb") == null)
                Console.WriteLine("Warning: cannot load Mono.Cecil.Pdb.dll");

            if (Type.GetType("Mono.Cecil.Mdb.MdbReaderProvider, Mono.Cecil.Mdb") == null)
                Console.WriteLine("Warning: cannot load Mono.Cecil.Mdb.dll");
        }

        ModuleDefinition module = null;

        try
        {
            var myResolver = new MyAssemblyResolver(file);

            module = ModuleDefinition.ReadModule(file, myResolver.Parameters);

            myResolver.ConfigureDotNetCoreDirectories(module);
        }
        catch (BadImageFormatException e)
        {
            throw new JavaException($"input file '{(file as FileStream)?.Name}' "
                                   + "is not a valid .Net assembly: " + e.Message, null);
        }

        try
        {
            module.ReadSymbols();
        }
        catch (Mono.Cecil.Cil.SymbolsNotFoundException)
        {
        }

        return module;
    }



    static string ResolveInputPath(String inputPath)
    {
        if (inputPath.StartsWith("**/"))
        {
            if (    inputPath.EndsWith(".dll", true, null)
                 || inputPath.EndsWith(".exe", true, null))
            {
                var assembly = System.Reflection.Assembly.GetAssembly(typeof(object));
                var netDir = Path.GetDirectoryName(assembly?.Location);
                if (! string.IsNullOrEmpty(netDir))
                {
                    var newPath = netDir + "/" + inputPath.Substring(2);
                    if (File.Exists(newPath))
                        return newPath;
                }
            }
            else
            {
                var javaDir = Environment.GetEnvironmentVariable("JAVA_HOME");
                if (! string.IsNullOrEmpty(javaDir))
                {
                    var newPath = javaDir + "/jre/lib/" + inputPath.Substring(2);
                    Console.WriteLine(newPath);
                    if (File.Exists(newPath))
                        return newPath;
                }
            }

            throw new JavaException($"unresolvable path '{inputPath}'", null);
        }

        return inputPath;
    }



    public class MyAssemblyResolver : DefaultAssemblyResolver
    {
        public ReaderParameters Parameters;
        string MainModuleFileName;
        Dictionary<string, AssemblyDefinition> Cache;
        List<string> Skipped;
        bool DotNetCore;

        public MyAssemblyResolver(Stream file) : base()
        {
            ConfigureSearchDirectories(file);

            Parameters = new ReaderParameters(ReadingMode.Deferred);
            Parameters.AssemblyResolver = this;

            Cache = new Dictionary<string, AssemblyDefinition>();
            Skipped = new List<string>();

            ResolveFailure += ResolveFailedCallback;
        }

        void ConfigureSearchDirectories(Stream file)
        {
            foreach (var dir in GetSearchDirectories())
                RemoveSearchDirectory(dir);    // remove defaults

            if (file is FileStream fileStream)
            {
                var dir = Path.GetDirectoryName(fileStream.Name);
                if (! string.IsNullOrEmpty(dir))
                    AddSearchDirectory(dir);
            }

            AddSearchDirectory(Directory.GetCurrentDirectory());

            MainModuleFileName =
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            AddSearchDirectory(Path.GetDirectoryName(MainModuleFileName));

            foreach (var dir in Environment.GetEnvironmentVariable("PATH")
                                          ?.Split(Path.PathSeparator))
                AddSearchDirectory(dir);
        }

        public void ConfigureDotNetCoreDirectories(ModuleDefinition module)
        {
            foreach (var asmref in module.AssemblyReferences)
            {
                if (asmref.Name == "System.Runtime")
                {
                    DotNetCore = (asmref.Version > new Version(4, 0));
                    break;
                }
            }
            if (DotNetCore)
            {
                var dotNetDir = Environment.GetEnvironmentVariable("PROGRAMFILES");
                if (! string.IsNullOrEmpty(dotNetDir))
                {
                    dotNetDir += "\\DotNet\\Shared\\Microsoft.NETCore.App\\";
                    if (Directory.Exists(dotNetDir))
                    {
                        var dirList = new List<String>(Directory.GetDirectories(dotNetDir));
                        dirList.Sort();
                        dirList.Reverse();
                        foreach (var dir in dirList)
                            AddSearchDirectory(dir);
                    }
                }
            }
        }

        protected override AssemblyDefinition SearchDirectory(AssemblyNameReference name,
                                                              IEnumerable<string> directories,
                                                              ReaderParameters parameters)
        {
            foreach (var directory in directories)
            {
                // check each directory in the search path for the assembly
                // as a file named assembly.DLL or assembly.EXE.  note that
                // the DLL silently takes precedence over the EXE

                string file = Path.Combine(directory, name.Name + ".dll");
                bool exists = File.Exists(file);
                if (! exists)
                {
                    file = Path.Combine(directory, name.Name + ".exe");
                    exists = File.Exists(file);
                }

                if (exists)
                {
                    try
                    {
                        var assembly = ModuleDefinition.ReadModule(file, Parameters).Assembly;
                        if (assembly.Name.Version >= name.Version)
                            return assembly;
                        Skipped.Add(assembly.FullName);
                    } catch (System.BadImageFormatException)
                    {
                        continue;
                    }
                }
            }
            return null;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
            => this.Resolve(name, null);

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (! Cache.TryGetValue(name.FullName, out var assembly))
            {
                Skipped.Clear();
                assembly = base.Resolve(name, Parameters);
                if (assembly != null)
                    Cache[assembly.FullName] = assembly;
            }
            return assembly;
        }

        AssemblyDefinition ResolveFailedCallback(object sender, AssemblyNameReference name)
        {
            var s = $"could not resolve assembly '{name}'"
                  + SpaceFlint.CilToJava.CilMain.Where.ToString()
                  + ".\n\nSearched in:";

            foreach (var dir in GetSearchDirectories())
                s += "    " + dir;

            if (Skipped.Count != 0)
            {
                s += "\n\nSkipped assemblies:";
                foreach (var skip in Skipped)
                    s += "    " + skip;
            }

            s += "\n\nYou may add directories to the PATH environment variable"
              +  " to include them in the search.";

            if (DotNetCore)
            {
                s += "\nFor a .Net Core application, you may need to add the shared assemblies directory:"
                  +  "\n    C:\\Program Files\\DotNet\\Shared\\Microsoft.NETCore.App\\3.0.0";
            }

            throw new JavaException(s, null);
        }

    }

}
