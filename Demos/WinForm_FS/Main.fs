
module SpaceFlint.Demos.WinForm_FS
open SpaceFlint.Demos

[<EntryPoint>]
let main argv =
    new WinForm (new System.Action<HAL>(SpaceFlint.Demos.Points.myInitialize), "F#") |> ignore
    0
