
module Cmdline

type Cmdline = {
    Inputs: string list
    Output: string
    Roots:  string list
}

let private exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName

let private usage () =
    printfn "usage: %s inputfile [inputfile2..] outputfile :keepclass [:keepclass2..]" exeName
    printfn "Merges one or more input JARs into one output JAR, while pruning unreferenced classes."
    None

let private parseArgsList (args: string list) =
    let (classes, args) = args |> List.partition (fun arg -> arg.Length > 1 && arg.StartsWith ":")
    let (files, args) = args |> List.partition (fun arg -> arg.Length > 0 && not (arg.StartsWith ":"))
    if files.Length >= 2 && classes.Length >= 1 && args.Length = 0
    then Some { Inputs = List.take (files.Length - 1) files
                Output = List.last files
                Roots = classes |> List.map (fun arg -> arg.Substring 1)
              }
    else usage()

let print format =
    Printf.ksprintf (fun msg -> printfn "%s: %s" exeName msg) format

let parse args =
    if isNull args then usage ()
    else Array.toList args |> parseArgsList
