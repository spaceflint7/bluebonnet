
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SpaceFlint.Demos
{

    public class WinForm : Form, HAL
    {

        private Bitmap canvas;
        private Timer timer;
        private Random random;
        private Action frameCallback;

        public WinForm(Action<HAL> initFunc, string suffix)
        {
            random = new Random();

            Text = "Bluebonnet Demo - WinForm - " + suffix;
            ClientSize = new Size(640, 480);
            OnResize(null);

            initFunc(this);

            timer = new Timer();
            timer.Tick += MyTimer;
            timer.Interval = 16;
            timer.Start();

            Application.Run(this);
        }

        protected override void OnResize(EventArgs e)
        {
            if (canvas == null || ClientSize != canvas.Size)
                canvas = new Bitmap(ClientSize.Width, ClientSize.Height);
        }

        private void MyTimer(object sender, EventArgs e)
        {
            frameCallback();

            var g = CreateGraphics();
            g.DrawImage(canvas, 0, 0);
            g.Dispose();
        }

        void HAL.Frame(Action callback) => frameCallback = callback;
        void HAL.Pixel(float x, float y, float r, float g, float b)
        {
            var x1 = Clamp(x, canvas.Width);
            var y1 = Clamp(y, canvas.Height);
            int r1 = Clamp(r, 256);
            int g1 = Clamp(g, 256);
            int b1 = Clamp(b, 256);
            canvas.SetPixel(x1, y1, Color.FromArgb(255, r1, g1, b1));

            int Clamp(float a, int b) => System.Math.Min(System.Math.Max((int) (a * b), 0), b - 1);
        }
        float HAL.Random() => (float) random.NextDouble();

    }

}
