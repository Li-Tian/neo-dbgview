using Neo.Core;
using System;
using NoDbgViewTR;

namespace Neo.Wallets
{
    public class Coin : IEquatable<Coin>
    {
        public CoinReference Reference;
        public TransactionOutput Output;
        public CoinState State;

        private string _address = null;
        public string Address
        {
            get
            {
                if (_address == null)
                {
                    _address = Wallet.ToAddress(Output.ScriptHash);
                }
                return _address;
            }
        }

        public bool Equals(Coin other)
        {
            TR.Enter();
            if (ReferenceEquals(this, other)) return TR.Exit(true);
            if (ReferenceEquals(null, other)) return TR.Exit(false);
            return TR.Exit(Reference.Equals(other.Reference));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            return TR.Exit(Equals(obj as Coin));
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(Reference.GetHashCode());
        }
    }
}
