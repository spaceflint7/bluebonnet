
namespace SpaceFlint.Demos

    type HAL =

        // void HAL::Frame(System.Action frameCallback)
        abstract member Frame : frameCallback:System.Action -> unit

        // void HAL::Pixel(float x, float y, float r, float g, float b)
        abstract member Pixel : x:single * y:single * r:single * g:single * b:single -> unit

        // float HAL::Random()
        abstract member Random : unit -> single
