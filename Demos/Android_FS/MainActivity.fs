
namespace com.spaceflint.bluebonnet.fsharp
open SpaceFlint.Demos

type MainActivity() =
    inherit android.app.Activity()

    let initTimer (view : android.view.View) =
        let mutable runnable : java.lang.Runnable = null
        runnable <- (new java.lang.Runnable.Delegate (fun () ->
                view.invalidate()
                view.postDelayed(runnable, 16L) |> ignore
            )).AsInterface()
        view.postDelayed(runnable, 16L) |> ignore
        ()

    override this.onCreate(savedInstanceState: android.os.Bundle) =
        base.onCreate(savedInstanceState)

        base.getWindow().setFlags(android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN,
                                  android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN)

        base.requestWindowFeature(android.view.Window.FEATURE_NO_TITLE) |> ignore

        let canvasView = new CanvasView(this)
        this.setContentView canvasView
        canvasView.requestFocus() |> ignore

        SpaceFlint.Demos.Points.myInitialize canvasView

        initTimer canvasView

and CanvasView(context) =
    inherit android.view.View(context)

    let paint = new android.graphics.Paint()
    let mutable frameCallback : System.Action = null
    let mutable frameCanvas : android.graphics.Canvas = null
    let mutable frameSize : (int * int) = (0, 0)

    do
        paint.setColor(android.graphics.Color.RED)
        paint.setAntiAlias(true)
        paint.setStrokeWidth(4.0f)
        paint.setTextSize(30.0f)

    let drawCenter text yMultiply =

        let r = new android.graphics.Rect()
        frameCanvas.getClipBounds(r) |> ignore
        paint.setTextAlign(android.graphics.Paint.Align.LEFT)
        paint.getTextBounds(string text, 0, (string text).Length, r)
        let (frameWidth, frameHeight) = frameSize
        let x = single frameWidth / 2.0f
              - single (r.width()) / 2.0f
              - single (r.left)
        let y = single frameHeight / 2.0f
              + single (r.height()) * single yMultiply / 2.0f
              - single r.bottom
        frameCanvas.drawText(text, x, y, paint)

    override this.onDraw(canvas) =

        frameCanvas <- canvas
        frameSize <- (canvas.getWidth(), canvas.getHeight())

        paint.setColor(0x7FFF0000)
        drawCenter "Bluebonnet Demo" -4.0f
        drawCenter "Android - F#"     4.0f

        frameCallback.Invoke()

    interface HAL with

        member this.Frame frameCallbackArg =

            frameCallback <- frameCallbackArg
            ()

        member this.Pixel (x, y, r, g, b) =

            let clamp (a:single) b = System.Math.Min(System.Math.Max(int (a * single b), 0), b - 1)
            let (frameWidth, frameHeight) = frameSize
            let x1 = clamp x frameWidth
            let y1 = clamp y frameHeight
            let r1 = clamp r 256
            let g1 = clamp g 256
            let b1 = clamp b 256
            paint.setColor((0xFF <<< 24) ||| (r1 <<< 16) ||| (g1 <<< 8) ||| b1)
            frameCanvas.drawPoint(single x1, single y1, paint)

        member this.Random () = single (java.lang.Math.random())
