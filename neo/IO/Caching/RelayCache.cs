﻿using Neo.Network;
using DbgViewTR;

namespace Neo.IO.Caching
{
    internal class RelayCache : FIFOCache<UInt256, IInventory>
    {
        public RelayCache(int max_capacity)
            : base(max_capacity)
        {
        }

        protected override UInt256 GetKeyForItem(IInventory item)
        {
            TR.Enter();
            return TR.Exit(item.Hash);
        }
    }
}
