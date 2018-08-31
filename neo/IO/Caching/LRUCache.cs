using System;
using NoDbgViewTR;

namespace Neo.IO.Caching
{
    internal abstract class LRUCache<TKey, TValue> : Cache<TKey, TValue>
    {
        public LRUCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override void OnAccess(CacheItem item)
        {
            TR.Enter();
            item.Time = DateTime.Now;
            TR.Exit();
        }
    }
}
