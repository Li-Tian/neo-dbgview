using Neo.VM;
using DbgViewTR;

namespace Neo.SmartContract
{
    internal class StorageContext : IInteropInterface
    {
        public UInt160 ScriptHash;
        public bool IsReadOnly;

        public byte[] ToArray()
        {
            TR.Enter();
            return TR.Exit(ScriptHash.ToArray());
        }
    }
}
