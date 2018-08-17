using Neo.Core;
using Neo.VM;
using System.Collections.Generic;
using DbgViewTR;

namespace Neo.SmartContract.Iterators
{
    internal class StorageIterator : IIterator
    {
        private readonly IEnumerator<KeyValuePair<StorageKey, StorageItem>> enumerator;

        public StorageIterator(IEnumerator<KeyValuePair<StorageKey, StorageItem>> enumerator)
        {
            TR.Enter();
            this.enumerator = enumerator;
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            enumerator.Dispose();
            TR.Exit();
        }

        public StackItem Key()
        {
            TR.Enter();
            return TR.Exit(enumerator.Current.Key.Key);
        }

        public bool Next()
        {
            TR.Enter();
            return TR.Exit(enumerator.MoveNext());
        }

        public StackItem Value()
        {
            TR.Enter();
            return TR.Exit(enumerator.Current.Value.Value);
        }
    }
}
