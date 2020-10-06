
namespace system
{

    public class WeakReference
    {
        [java.attr.RetainType] private java.lang.@ref.WeakReference _ref;

        public WeakReference(object target)
            => _ref = new java.lang.@ref.WeakReference(target);

        public WeakReference(object target, bool trackResurrection)
        {
            if (trackResurrection)
                throw new System.PlatformNotSupportedException();
            _ref = new java.lang.@ref.WeakReference(target);
        }

        public virtual object Target
        {
            get => _ref?.get();
            set => _ref = new java.lang.@ref.WeakReference(value);
        }

        public virtual bool IsAlive => _ref?.get() != null;

        public virtual bool TrackResurrection => false;
    }


    public sealed class WeakReference<T> where T : class
    {
        [java.attr.RetainType] private java.lang.@ref.WeakReference _ref;

        public WeakReference(T target)
            => _ref = new java.lang.@ref.WeakReference(target);

        public WeakReference(T target, bool trackResurrection)
        {
            if (trackResurrection)
                throw new System.PlatformNotSupportedException();
            _ref = new java.lang.@ref.WeakReference(target);
        }

        public bool TryGetTarget(out T target)
        {
            var t = _ref?.get();
            target = (T) t;
            return (t != null);
        }

        public void SetTarget(T target)
            => _ref = new java.lang.@ref.WeakReference(target);
    }

}
