﻿using Neo.IO.Caching;
using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class DbMetaDataCache<T> : MetaDataCache<T> where T : class, ISerializable, new()
    {
        private DB db;
        private byte prefix;

        public DbMetaDataCache(DB db, byte prefix, Func<T> factory = null)
            : base(factory)
        {
            TR.Log();
            this.db = db;
            this.prefix = prefix;
        }

        public void Commit(WriteBatch batch)
        {
            TR.Enter();
            TR.Log(State);
            switch (State)
            {
                case TrackState.Added:
                case TrackState.Changed:
                    batch.Put(prefix, Item.ToArray());
                    break;
                case TrackState.Deleted:
                    batch.Delete(prefix);
                    break;
            }
            TR.Exit();
        }

        protected override T TryGetInternal()
        {
            TR.Enter();
            if (!db.TryGet(ReadOptions.Default, prefix, out Slice slice))
            {
                return TR.Exit((T)null);
            }
            return TR.Exit(slice.ToArray().AsSerializable<T>());
        }
    }
}
