using Neo.VM;
using System.Collections.Generic;
using DbgViewTR;

namespace Neo.SmartContract.Iterators
{
    internal class MapWrapper : IIterator
    {
        private readonly IEnumerator<KeyValuePair<StackItem, StackItem>> enumerator;

        public MapWrapper(IEnumerable<KeyValuePair<StackItem, StackItem>> map)
        {
            TR.Enter();
            this.enumerator = map.GetEnumerator();
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
            return TR.Exit(enumerator.Current.Key);
        }

        public bool Next()
        {
            TR.Enter();
            return TR.Exit(enumerator.MoveNext());
        }

        public StackItem Value()
        {
            TR.Enter();
            return TR.Exit(enumerator.Current.Value);
        }
    }
}
