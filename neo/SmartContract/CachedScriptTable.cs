using Neo.Core;
using Neo.IO.Caching;
using Neo.VM;
using DbgViewTR;

namespace Neo.SmartContract
{
    internal class CachedScriptTable : IScriptTable
    {
        private DataCache<UInt160, ContractState> contracts;

        public CachedScriptTable(DataCache<UInt160, ContractState> contracts)
        {
            TR.Enter();
            this.contracts = contracts;
            TR.Exit();
        }

        byte[] IScriptTable.GetScript(byte[] script_hash)
        {
            TR.Enter();
            return TR.Exit(contracts[new UInt160(script_hash)].Script);
        }

        public ContractState GetContractState(byte[] script_hash)
        {
            TR.Enter();
            return TR.Exit(contracts[new UInt160(script_hash)]);
        }
    }
}
