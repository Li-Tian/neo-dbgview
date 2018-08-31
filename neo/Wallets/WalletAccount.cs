using Neo.SmartContract;
using NoDbgViewTR;

namespace Neo.Wallets
{
    public abstract class WalletAccount
    {
        public readonly UInt160 ScriptHash;
        public string Label;
        public bool IsDefault;
        public bool Lock;
        public Contract Contract;

        public string Address => Wallet.ToAddress(ScriptHash);
        public abstract bool HasKey { get; }
        public bool WatchOnly => Contract == null;

        public abstract KeyPair GetKey();

        protected WalletAccount(UInt160 scriptHash)
        {
            TR.Enter();
            this.ScriptHash = scriptHash;
            TR.Exit();
        }
    }
}
