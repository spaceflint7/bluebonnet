
using SpaceFlint.Demos;

namespace com.spaceflint.bluebonnet.csharp
{

    public class MainActivity : android.app.Activity
    {

        protected override void onCreate(android.os.Bundle savedInstanceState)
        {
            base.onCreate(savedInstanceState);

            getWindow().setFlags(android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN,
                                 android.view.WindowManager.LayoutParams.FLAG_FULLSCREEN);
            requestWindowFeature(android.view.Window.FEATURE_NO_TITLE);

            var canvasView = new CanvasView(this);
            setContentView(canvasView);
            canvasView.requestFocus();

            Points_CS.Initialize(canvasView);

            InitTimer(canvasView);
        }

        void InitTimer(CanvasView view)
        {
            const int TIMER_RATE = 16;
            java.lang.Runnable runnable = null;
            runnable = ((java.lang.Runnable.Delegate) (() =>
            {
                view.invalidate();
                view.postDelayed(runnable, TIMER_RATE);

            })).AsInterface();
            view.postDelayed(runnable, TIMER_RATE);
        }
    }

    public class CanvasView : android.view.View, HAL {

        System.Action frameCallback;
        android.graphics.Paint paint;
        android.graphics.Canvas frameCanvas;
        int frameWidth, frameHeight;

        public CanvasView(android.content.Context context) : base(context)
        {
            paint = new android.graphics.Paint();
            paint.setColor(android.graphics.Color.RED);
            paint.setAntiAlias(true);
            paint.setStrokeWidth(4);
            paint.setTextSize(30);
        }

        protected override void onDraw(android.graphics.Canvas canvas)
        {
            (frameCanvas, frameWidth, frameHeight) =
                (canvas, canvas.getWidth(), canvas.getHeight());

            paint.setColor(0x7FFF0000);
            drawCenter("Bluebonnet Demo", -4f);
            drawCenter("Android - C#", 4f);

            frameCallback();
        }

        void drawCenter(string text, float yMultiply)
        {
            var r = new android.graphics.Rect();
            frameCanvas.getClipBounds(r);
            paint.setTextAlign(android.graphics.Paint.Align.LEFT);
            paint.getTextBounds(text, 0, text.Length, r);
            float x = frameWidth / 2f - r.width() / 2f - r.left;
            float y = frameHeight / 2f + (r.height() * yMultiply) / 2f - r.bottom;
            frameCanvas.drawText(text, x, y, paint);
        }

        void HAL.Frame(System.Action callback) => frameCallback = callback;
        void HAL.Pixel(float x, float y, float r, float g, float b)
        {
            var x1 = Clamp(x, frameWidth);
            var y1 = Clamp(y, frameHeight);
            int r1 = Clamp(r, 256);
            int g1 = Clamp(g, 256);
            int b1 = Clamp(b, 256);
            paint.setColor((0xFF << 24) | (r1 << 16) | (g1 << 8) | b1);
            frameCanvas.drawPoint(x1, y1, paint);
            int Clamp(float a, int b) => System.Math.Min(System.Math.Max((int) (a * b), 0), b - 1);
        }
        float HAL.Random() => (float) java.lang.Math.random();

    }

}
