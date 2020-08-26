
namespace SpaceFlint.Demos
{

    public class Points_CS
    {

        private HAL hal;



        public static void Initialize(HAL hal)
        {
            hal.Frame((new Points_CS() { hal = hal }).Frame);
        }



        void Frame()
        {
            for (int i = 0; i < 1000; i++)
            {
                hal.Pixel(  /*x,y*/ hal.Random(), hal.Random(),
                          /*r,g,b*/ hal.Random(), hal.Random(), hal.Random());
            }
        }

    }

}
