using System;
using System.Collections.Generic;
using System.Linq;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public static class Helper
    {
        public static void Delete(this WriteBatch batch, byte prefix, ISerializable key)
        {
            TR.Enter();
            batch.Delete(SliceBuilder.Begin(prefix).Add(key));
            TR.Exit();
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, byte prefix) where T : class, ISerializable, new()
        {
            TR.Enter();
            return TR.Exit(Find(db, options, SliceBuilder.Begin(prefix), (k, v) => v.ToArray().AsSerializable<T>()));
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, Slice prefix, Func<Slice, Slice, T> resultSelector)
        {
            TR.Enter();
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(prefix); it.Valid(); it.Next())
                {
                    Slice key = it.Key();
                    byte[] x = key.ToArray();
                    byte[] y = prefix.ToArray();
                    if (x.Length < y.Length) break;
                    if (!x.Take(y.Length).SequenceEqual(y)) break;
                    yield return TR.Exit(resultSelector(key, it.Value()));
                }
            }
            TR.Exit();
        }

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            TR.Enter();
            return TR.Exit(db.Get(options, SliceBuilder.Begin(prefix).Add(key)).ToArray().AsSerializable<T>());
        }

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector)
        {
            TR.Enter();
            return TR.Exit(resultSelector(db.Get(options, SliceBuilder.Begin(prefix).Add(key))));
        }

        public static void Put(this WriteBatch batch, byte prefix, ISerializable key, ISerializable value)
        {
            TR.Enter();
            batch.Put(SliceBuilder.Begin(prefix).Add(key), value.ToArray());
            TR.Exit();
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            TR.Enter();
            Slice slice;
            if (!db.TryGet(options, SliceBuilder.Begin(prefix).Add(key), out slice))
            {
                TR.Exit();
                return null;
            }
            return TR.Exit(slice.ToArray().AsSerializable<T>());
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector) where T : class
        {
            TR.Enter();
            Slice slice;
            if (!db.TryGet(options, SliceBuilder.Begin(prefix).Add(key), out slice))
            {
                TR.Exit();
                return null;
            }
            return TR.Exit(resultSelector(slice));
        }
    }
}
