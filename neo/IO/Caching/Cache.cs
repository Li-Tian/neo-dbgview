using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DbgViewTR;

namespace Neo.IO.Caching
{
    internal abstract class Cache<TKey, TValue> : ICollection<TValue>, IDisposable
    {
        protected class CacheItem
        {
            public TKey Key;
            public TValue Value;
            public DateTime Time;

            public CacheItem(TKey key, TValue value)
            {
                this.Key = key;
                this.Value = value;
                this.Time = DateTime.Now;
            }
        }

        public readonly object SyncRoot = new object();
        protected readonly Dictionary<TKey, CacheItem> InnerDictionary = new Dictionary<TKey, CacheItem>();
        private readonly int max_capacity;

        public TValue this[TKey key]
        {
            get
            {
                lock (SyncRoot)
                {
                    if (!InnerDictionary.TryGetValue(key, out CacheItem item)) throw new KeyNotFoundException();
                    OnAccess(item);
                    return item.Value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return InnerDictionary.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public Cache(int max_capacity)
        {
            TR.Enter();
            this.max_capacity = max_capacity;
            TR.Exit();
        }

        public void Add(TValue item)
        {
            TR.Enter();
            TKey key = GetKeyForItem(item);
            lock (SyncRoot)
            {
                AddInternal(key, item);
            }
            TR.Exit();
        }

        private void AddInternal(TKey key, TValue item)
        {
            TR.Enter();
            if (InnerDictionary.TryGetValue(key, out CacheItem cacheItem))
            {
                OnAccess(cacheItem);
            }
            else
            {
                if (InnerDictionary.Count >= max_capacity)
                {
                    //TODO: 对PLINQ查询进行性能测试，以便确定此处使用何种算法更优（并行或串行）
                    foreach (CacheItem item_del in InnerDictionary.Values.AsParallel().OrderBy(p => p.Time).Take(InnerDictionary.Count - max_capacity + 1))
                    {
                        RemoveInternal(item_del);
                    }
                }
                InnerDictionary.Add(key, new CacheItem(key, item));
            }
            TR.Exit();
        }

        public void AddRange(IEnumerable<TValue> items)
        {
            TR.Enter();
            lock (SyncRoot)
            {
                foreach (TValue item in items)
                {
                    TKey key = GetKeyForItem(item);
                    AddInternal(key, item);
                }
            }
            TR.Exit();
        }

        public void Clear()
        {
            TR.Enter();
            lock (SyncRoot)
            {
                foreach (CacheItem item_del in InnerDictionary.Values.ToArray())
                {
                    RemoveInternal(item_del);
                }
            }
            TR.Exit();
        }

        public bool Contains(TKey key)
        {
            TR.Enter();
            lock (SyncRoot)
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return TR.Exit(false);
                OnAccess(cacheItem);
                return TR.Exit(true);
            }
        }

        public bool Contains(TValue item)
        {
            TR.Enter();
            return TR.Exit(Contains(GetKeyForItem(item)));
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            TR.Enter();
            if (array == null)
            {
                TR.Exit();
                throw new ArgumentNullException();
            }
            if (arrayIndex < 0)
            {
                TR.Exit();
                throw new ArgumentOutOfRangeException();
            }
            if (arrayIndex + InnerDictionary.Count > array.Length)
            {
                TR.Exit();
                throw new ArgumentException();
            }
            foreach (TValue item in this)
            {
                array[arrayIndex++] = item;
            }
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            Clear();
            TR.Exit();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            TR.Enter();
            lock (SyncRoot)
            {
                foreach (TValue item in InnerDictionary.Values.Select(p => p.Value))
                {
                    yield return TR.Exit(item);
                }
            }
            TR.Exit();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            TR.Enter();
            return TR.Exit(GetEnumerator());
        }

        protected abstract TKey GetKeyForItem(TValue item);

        public bool Remove(TKey key)
        {
            TR.Enter();
            lock (SyncRoot)
            {
                if (!InnerDictionary.TryGetValue(key, out CacheItem cacheItem)) return TR.Exit(false);
                RemoveInternal(cacheItem);
                return TR.Exit(true);
            }
        }

        protected abstract void OnAccess(CacheItem item);

        public bool Remove(TValue item)
        {
            TR.Enter();
            return TR.Exit(Remove(GetKeyForItem(item)));
        }

        private void RemoveInternal(CacheItem item)
        {
            TR.Enter();
            InnerDictionary.Remove(item.Key);
            IDisposable disposable = item.Value as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
            TR.Exit();
        }

        public bool TryGet(TKey key, out TValue item)
        {
            TR.Enter();
            lock (SyncRoot)
            {
                if (InnerDictionary.TryGetValue(key, out CacheItem cacheItem))
                {
                    OnAccess(cacheItem);
                    item = cacheItem.Value;
                    return TR.Exit(true);
                }
            }
            item = default(TValue);
            return TR.Exit(false);
        }
    }
}
