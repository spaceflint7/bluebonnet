
module SpaceFlint.Demos.Points
open SpaceFlint.Demos

let myFrameFunc hal =
    let x0 = (hal :> HAL).Random()
    let y0 = (hal :> HAL).Random()
    let radius = (hal :> HAL).Random()
    // iterate on a list [1..360], just to force dependancy on FSharp.Core.dll
    for i in [1..360] do
        let iRads = (float i) * System.Math.PI / 180.0
        let x1 = x0 + (single (System.Math.Sin iRads)) * radius
        let y1 = x1 + (single (System.Math.Cos iRads)) * radius
        (hal :> HAL).Pixel(  x1, y1,    hal.Random(), hal.Random(), hal.Random())
            |> ignore
    ()

let myInitialize hal =
    (hal :> HAL).Frame (fun () -> myFrameFunc hal)
