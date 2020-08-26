
namespace system.threading
{

    public struct SpinWait
    {

        public void SpinOnce() => java.lang.Thread.yield();

    }

}
