using System;
using DbgViewTR;

namespace Neo.IO.Caching
{
    public abstract class MetaDataCache<T> where T : class, ISerializable, new()
    {
        protected T Item;
        protected TrackState State;
        private Func<T> factory;

        protected abstract T TryGetInternal();

        protected MetaDataCache(Func<T> factory)
        {
            TR.Enter();
            this.factory = factory;
            TR.Exit();
        }

        public T Get()
        {
            TR.Enter();
            if (Item == null)
            {
                Item = TryGetInternal();
            }
            if (Item == null)
            {
                Item = factory?.Invoke() ?? new T();
                State = TrackState.Added;
            }
            return TR.Exit(Item);
        }

        public T GetAndChange()
        {
            TR.Enter();
            T item = Get();
            if (State == TrackState.None)
                State = TrackState.Changed;
            return TR.Exit(item);
        }
    }
}
