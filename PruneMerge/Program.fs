
module PruneMerge

open System
open System.IO
open System.IO.Compression
open System.Collections.Generic
open SpaceFlint.JavaBinary

let private SuccessExitCode: int = 0
let private ErrorExitCode: int = 1
exception IoError of string * Exception

let readJar path =
    try
        use file = File.OpenRead path
        use jar = new ZipArchive (file, ZipArchiveMode.Read)
        jar.Entries |> Seq.choose (fun entry -> JavaReader.ReadClassEx (entry, false) |> Option.ofObj)
                    |> Seq.toList
    with
    | ex -> raise (IoError ((sprintf "reading '%s'" path), ex))

let readJarsIntoMap inputs =
    let known = HashSet()
    inputs |> List.map readJar |> List.concat
           |> List.map (fun clsex ->
                            let cls = clsex.JavaClass
                            if not (known.Add cls.Name)
                                then failwithf "duplicate class '%s'" cls.Name
                                else (cls.Name, clsex))
           |> Map.ofList

let writeListIntoJar (allClasses: Map<string, JavaReader.JavaClassEx>) keepClasses path =
    try
        use file = File.Open (path, FileMode.CreateNew)
        use jar = new ZipArchive (file, ZipArchiveMode.Update)
        keepClasses |> Set.iter (fun name ->
            let bytes = (allClasses |> Map.find name).RawBytes
            let name = name.Replace('.', '/') + ".class"
            let entry = jar.CreateEntry (name, CompressionLevel.Optimal)
            use entryStream = entry.Open()
            entryStream.Write(bytes, 0, bytes.Length)
        )
    with
    | ex -> raise (IoError ((sprintf "writing '%s'" path), ex))

let shrink (allClasses: Map<string, JavaReader.JavaClassEx>) (filterNames: string list) =

    let getClassRefName (constpool: JavaConstantPool) idx =
        match constpool.Get (idx) with
        | :? JavaConstant.Class as c ->
                match constpool.Get (int (c.stringIndex)) with
                | :? JavaConstant.Utf8 as u -> u.str
                | _ -> ""
        | _ -> ""

    let extractClassRefs name (constpool: JavaConstantPool) =
        let mutable refs = List.empty
        for i = constpool.Count - 1 downto 0 do
            let name2 = getClassRefName constpool i
            if name2.Length > 0
                then refs <- name2.Replace('/', '.') :: refs
        refs

    let rec addClass name keep important =
        if Set.contains name keep then keep
        else match allClasses |> Map.tryFind name with
             | None -> if important 
                       then failwithf "class '%s' not found in input" name
                       else keep
             | Some clsex -> extractClassRefs name clsex.Constants
                             |> List.fold (fun keep name -> addClass name keep false)
                                          (Set.add name keep)

    filterNames |> List.fold (fun keep name -> addClass name keep true) Set.empty

[<EntryPoint>]
let main args =
    Cmdline.parse args |> Option.exists (fun cmd ->
        if File.Exists cmd.Output then
            Cmdline.print "error: output file '%s' already exists" cmd.Output
            false
        else try
                let allClasses = readJarsIntoMap cmd.Inputs
                let keepClasses = shrink allClasses cmd.Roots
                Cmdline.print "%d classes in input, %d classes in output" allClasses.Count keepClasses.Count 
                writeListIntoJar allClasses keepClasses cmd.Output
                true
             with
             | Failure msg          -> Cmdline.print "error: %s" msg; false
             | IoError (path, ex)   -> Cmdline.print "error %s: %s: %s" path (ex.GetType().Name) ex.Message ; false
             | (* any *) ex         -> Cmdline.print "%s: %s" (ex.GetType().Name) ex.Message ; false
    ) |> (fun ok -> if ok then 0 else 1) // program exit code
