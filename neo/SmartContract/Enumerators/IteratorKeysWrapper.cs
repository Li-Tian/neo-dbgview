using Neo.SmartContract.Iterators;
using Neo.VM;
using DbgViewTR;

namespace Neo.SmartContract.Enumerators
{
    internal class IteratorKeysWrapper : IEnumerator
    {
        private readonly IIterator iterator;

        public IteratorKeysWrapper(IIterator iterator)
        {
            TR.Enter();
            this.iterator = iterator;
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            iterator.Dispose();
            TR.Exit();
        }

        public bool Next()
        {
            TR.Enter();
            return TR.Exit(iterator.Next());
        }

        public StackItem Value()
        {
            TR.Enter();
            return TR.Exit(iterator.Key());
        }
    }
}
