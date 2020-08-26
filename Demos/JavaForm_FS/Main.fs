
module SpaceFlint.Demos.JavaForm_FS
open SpaceFlint.Demos

// void main(String args[]) is the entrypoint java recognizes
let main (argv : string[]) : unit =
    new JavaForm (new System.Action<HAL>(SpaceFlint.Demos.Points.myInitialize), "F#") |> ignore
    ()

// prevent compiler from warning about missing entrypoint
[<EntryPoint>]
let dummy_main argv = 0
