
namespace system.runtime.compilerservices
{

    // note that unlike .Net counterpart, this is not an ephemeron table,
    // and value objects that reference keys may cause the table to leak.

    public class ConditionalWeakTable
    {



        protected sealed class WeakKeyRef : java.lang.@ref.WeakReference
        {
            [java.attr.RetainType] int hash;
            [java.attr.RetainType] public object value;

            public WeakKeyRef(object key, java.lang.@ref.ReferenceQueue refq)
                : base(key, refq)
            {
                if (key == null)
                    throw new System.ArgumentNullException(nameof(key));
                hash = java.lang.System.identityHashCode(key);
            }

            public override int GetHashCode() => hash;

            public override bool Equals(object obj) =>
                object.ReferenceEquals(get(), (obj as WeakKeyRef)?.get());
        }



        [java.attr.RetainType] protected readonly java.util.HashMap map = new java.util.HashMap();

        [java.attr.RetainType] protected readonly java.lang.@ref.ReferenceQueue refq = new java.lang.@ref.ReferenceQueue();

        [java.attr.RetainType] protected readonly java.util.concurrent.locks.ReentrantReadWriteLock.ReadLock readLock;
        [java.attr.RetainType] protected readonly java.util.concurrent.locks.ReentrantReadWriteLock.WriteLock writeLock;



        public ConditionalWeakTable()
        {
            java.util.concurrent.locks.ReadWriteLock rwlock =
                                            new java.util.concurrent.locks.ReentrantReadWriteLock();

            readLock = (java.util.concurrent.locks.ReentrantReadWriteLock.ReadLock) rwlock.readLock();
            writeLock = (java.util.concurrent.locks.ReentrantReadWriteLock.WriteLock) rwlock.writeLock();
        }



        public object GetOrAdd(object key, object value)
        {
            var keyref = new WeakKeyRef(key, refq);

            writeLock.@lock();

            try
            {
                DiscardWriteLocked();

                object key2;
                var keyref2 = (WeakKeyRef) map.get(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    value = keyref2.value;
                }
                else if (value != null)
                {
                    keyref.value = value;
                    map.put(keyref, keyref);
                }
            }

            finally
            {
                writeLock.unlock();
            }

            return value;
        }



        protected void DiscardWriteLocked()
        {
            for (;;)
            {
                var weakref = (java.lang.@ref.WeakReference) refq.poll();
                if (weakref == null)
                    break;

                map.remove(weakref);
            }
        }



        protected void DiscardNotLocked()
        {
            var weakref = (java.lang.@ref.WeakReference) refq.poll();
            if (weakref != null)
            {
                writeLock.@lock();

                try
                {
                    for (;;)
                    {
                        map.remove(weakref);

                        weakref = (java.lang.@ref.WeakReference) refq.poll();
                        if (weakref == null)
                            break;
                    }
                }

                finally
                {
                    writeLock.unlock();
                }
            }
        }

    }



    public sealed class ConditionalWeakTable<TKey,TValue> : ConditionalWeakTable
           where TKey: class
           where TValue: class
    {

        public delegate TValue CreateValueCallback(TKey key);



        public void Add(TKey key, TValue value)
        {
            var keyref = new WeakKeyRef(key, refq);

            writeLock.@lock();

            try
            {
                DiscardWriteLocked();

                object key2;
                var keyref2 = (WeakKeyRef) map.get(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    throw new System.ArgumentException(nameof(key));
                }

                keyref.value = value;
                map.put(keyref, keyref);
            }

            finally
            {
                writeLock.unlock();
            }
        }



        public bool Remove(TKey key)
        {
            var keyref = new WeakKeyRef(key, refq);

            writeLock.@lock();
            bool removed = false;

            try
            {
                DiscardWriteLocked();

                object key2;
                var keyref2 = (WeakKeyRef) map.remove(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    removed = true;
                }
            }

            finally
            {
                writeLock.unlock();
            }

            return removed;
        }



        public bool TryGetValue(TKey key, out TValue value)
        {
            var keyref = new WeakKeyRef(key, refq);
            bool found = false;

            DiscardNotLocked();
            readLock.@lock();

            try
            {
                object key2;
                var keyref2 = (WeakKeyRef) map.get(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    value = (TValue) keyref2.value;
                    found = true;
                }
                else
                {
                    value = default(TValue);
                    found = false;
                }
            }

            finally
            {
                readLock.unlock();
            }

            return found;
        }



        public TValue GetValue(TKey key, CreateValueCallback createValueCallback)
        {
            if (createValueCallback == null)
                throw new System.ArgumentNullException(nameof(createValueCallback));

            var keyref = new WeakKeyRef(key, refq);

            object key2;
            WeakKeyRef keyref2;

            DiscardNotLocked();
            readLock.@lock();

            try
            {
                keyref2 = (WeakKeyRef) map.get(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    return (TValue) keyref2.value;
                }
            }

            finally
            {
                readLock.unlock();
            }

            var value = createValueCallback(key);

            writeLock.@lock();

            try
            {

                keyref2 = (WeakKeyRef) map.get(keyref);
                if (keyref2 != null && (key2 = keyref2.get()) != null)
                {
                    value = (TValue) keyref2.value;
                }
                else
                {
                    keyref.value = value;
                    map.put(keyref, keyref);
                }
            }

            finally
            {
                writeLock.unlock();
            }

            return value;
        }



        private TValue DefaultCreateValueCallback(TKey key)
        {
            return System.Activator.CreateInstance<TValue>();

        }



        public TValue GetOrCreateValue(TKey key)
        {
            return GetValue(key, DefaultCreateValueCallback);
        }

    }


}
