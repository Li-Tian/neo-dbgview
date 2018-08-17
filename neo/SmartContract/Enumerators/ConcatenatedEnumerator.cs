using Neo.VM;
using DbgViewTR;

namespace Neo.SmartContract.Enumerators
{
    internal class ConcatenatedEnumerator : IEnumerator
    {
        private readonly IEnumerator first, second;
        private IEnumerator current;

        public ConcatenatedEnumerator(IEnumerator first, IEnumerator second)
        {
            TR.Enter();
            this.current = this.first = first;
            this.second = second;
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            first.Dispose();
            second.Dispose();
            TR.Exit();
        }

        public bool Next()
        {
            TR.Enter();
            if (current.Next()) return TR.Exit(true);
            current = second;
            return TR.Exit(current.Next());
        }

        public StackItem Value()
        {
            TR.Enter();
            return TR.Exit(current.Value());
        }
    }
}
