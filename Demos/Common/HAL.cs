
namespace SpaceFlint.Demos
{

    // Hardware Abstraction Layer

    // this is the interface between the demo logic (e.g. the Points_CS class)
    // and the platform-specific wrapper.  we have a WinForms application for
    // the .Net wrapper (in the WinForm class), a Swing form (in the JavaForm
    // class), and an Android wrapper (in the Android project).

    public interface HAL
    {

        // the logic code calls this once during initialization
        void Frame(System.Action frameCallback);

        // draw a pixel on the screen, all values are between 0 and 1
        void Pixel(float x, float y, float r, float g, float b);

        // get a random number between 0 and 1
        float Random();

    }

}
