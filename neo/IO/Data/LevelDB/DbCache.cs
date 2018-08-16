using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using DbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private DB db;
        private WriteBatch batch;
        private byte prefix;

        public DbCache(DB db, byte prefix, WriteBatch batch = null)
        {
            TR.Enter();
            this.db = db;
            this.batch = batch;
            this.prefix = prefix;
            TR.Exit();
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            TR.Enter();
            batch?.Put(prefix, key, value);
            TR.Exit();
        }

        public override void DeleteInternal(TKey key)
        {
            TR.Enter();
            batch?.Delete(prefix, key);
            TR.Exit();
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            TR.Enter();
            return TR.Exit(db.Find(ReadOptions.Default, SliceBuilder.Begin(prefix).Add(key_prefix), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(1), v.ToArray().AsSerializable<TValue>())));
        }

        protected override TValue GetInternal(TKey key)
        {
            TR.Enter();
            return TR.Exit(db.Get<TValue>(ReadOptions.Default, prefix, key));
        }

        protected override TValue TryGetInternal(TKey key)
        {
            TR.Enter();
            return TR.Exit(db.TryGet<TValue>(ReadOptions.Default, prefix, key));
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            TR.Enter();
            batch?.Put(prefix, key, value);
            TR.Exit();
        }
    }
}
