
using java.awt.image;
using java.awt.@event;
using javax.swing;

namespace SpaceFlint.Demos
{

    public class JavaForm : JFrame, ComponentListener, HAL
    {

        BufferedImage canvas;
        private Timer timer;
        private System.Action frameCallback;

        public JavaForm(System.Action<HAL> initFunc, string suffix)
        {
            setTitle("Bluebonnet Demo - JavaForm - " + suffix);
            setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);

            setSize(640, 480);
            componentResized(null);
            addComponentListener(this);

            initFunc(this);

            ActionListener.Delegate timerDelegate = (ActionEvent e) =>
            {
                frameCallback();
                repaint();
            };

            timer = new Timer(16, timerDelegate.AsInterface());
            timer.start();

            add(new MyPane(this));
            setVisible(true);
        }

        //
        // methods implementing the java.awt.event.ComponentListener interface.
        // note the required [java.attr.RetainName] attribute, when implementing
        // methods from an imported java interface.
        //

        [java.attr.RetainName] public void componentHidden(ComponentEvent e) => componentResized(e);
        [java.attr.RetainName] public void componentMoved(ComponentEvent e) => componentResized(e);
        [java.attr.RetainName] public void componentShown(ComponentEvent e) => componentResized(e);
        [java.attr.RetainName] public void componentResized(ComponentEvent e)
        {
            if (    canvas == null
                 || canvas.getWidth() != getWidth()
                 || canvas.getHeight() != getHeight())
            {
                canvas = new BufferedImage(getWidth(), getHeight(), BufferedImage.TYPE_INT_ARGB);
            }
        }

        //
        // implement the HAL interface
        //

        void HAL.Frame(System.Action callback) => frameCallback = callback;
        void HAL.Pixel(float x, float y, float r, float g, float b)
        {
            var x1 = Clamp(x, canvas.getWidth());
            var y1 = Clamp(y, canvas.getHeight());
            int r1 = Clamp(r, 256);
            int g1 = Clamp(g, 256);
            int b1 = Clamp(b, 256);
            canvas.setRGB(x1, y1, (0xFF << 24) | (r1 << 16) | (g1 << 8) | b1);

            int Clamp(float a, int b) => System.Math.Min(System.Math.Max((int) (a * b), 0), b - 1);
        }
        float HAL.Random() => (float) java.lang.Math.random();

        class MyPane : JPanel
        {
            JavaForm form;
            public MyPane(JavaForm form) => this.form = form;
            protected override void paintComponent(java.awt.Graphics g)
            {
                base.paintComponent(g);
                g.drawImage(form.canvas, 0, 0, null);
            }
        }

    }

}
