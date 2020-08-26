
namespace system.text.regularexpressions
{

    [System.Serializable]
    public class CaptureCollection : System.Collections.ICollection
    {

        public int Count => 0;

        public Group this[int i] => throw new System.PlatformNotSupportedException();
        public void CopyTo(System.Array array, int index)
            => throw new System.PlatformNotSupportedException();

        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => throw new System.PlatformNotSupportedException();

        public System.Collections.IEnumerator GetEnumerator()
            => throw new System.PlatformNotSupportedException();
    }

}
