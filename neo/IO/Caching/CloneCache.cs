using System;
using System.Collections.Generic;
using DbgViewTR;

namespace Neo.IO.Caching
{
    internal class CloneCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private DataCache<TKey, TValue> innerCache;

        public CloneCache(DataCache<TKey, TValue> innerCache)
        {
            TR.Enter();
            this.innerCache = innerCache;
            TR.Exit();
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            TR.Enter();
            innerCache.Add(key, value);
            TR.Exit();
        }

        public override void DeleteInternal(TKey key)
        {
            TR.Enter();
            innerCache.Delete(key);
            TR.Exit();
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            TR.Enter();
            foreach (KeyValuePair<TKey, TValue> pair in innerCache.Find(key_prefix))
                yield return TR.Exit(new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Clone()));
        }

        protected override TValue GetInternal(TKey key)
        {
            TR.Enter();
            return TR.Exit(innerCache[key].Clone());
        }

        protected override TValue TryGetInternal(TKey key)
        {
            TR.Enter();
            return TR.Exit(innerCache.TryGet(key)?.Clone());
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            TR.Enter();
            innerCache.GetAndChange(key).FromReplica(value);
            TR.Exit();
        }
    }
}
