using Neo.VM;
using System.Collections.Generic;
using NoDbgViewTR;

namespace Neo.SmartContract.Enumerators
{
    internal class ArrayWrapper : IEnumerator
    {
        private readonly IEnumerator<StackItem> enumerator;

        public ArrayWrapper(IEnumerable<StackItem> array)
        {
            TR.Enter();
            this.enumerator = array.GetEnumerator();
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            enumerator.Dispose();
            TR.Exit();
        }

        public bool Next()
        {
            TR.Enter();
            return TR.Exit(enumerator.MoveNext());
        }

        public StackItem Value()
        {
            TR.Enter();
            return TR.Exit(enumerator.Current);
        }
    }
}
