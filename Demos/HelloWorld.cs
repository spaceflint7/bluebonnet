using System;
using System.Linq;
using System.Threading;

// namespace names are translated to lowercase
namespace SpaceFlint.Demos
{
    // class names preserve capitalization
    public class HelloWorld
    {
        // entrypoint when C# is running on the CLR
        static void Main(string[] args)
        {
            "Hello, World!"
                .AsParallel().WithDegreeOfParallelism(32)
                .Select((ch, idx)
                    => new { theChar = ch, theIndex = idx})
                .ForAll(x => {
                    Thread.Sleep(x.theIndex * 200);
                    Console.Write(x.theChar); });
        }

        // JVM entrypoint is a lowercase "main" method
        public static void main(string[] args) => Main(args);
    }
}
