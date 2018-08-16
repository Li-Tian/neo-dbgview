using System;
using DbgViewTR;

namespace Neo.Implementations.Wallets.NEP6
{
    internal class WalletLocker : IDisposable
    {
        private NEP6Wallet wallet;

        public WalletLocker(NEP6Wallet wallet)
        {
            TR.Enter();
            this.wallet = wallet;
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            wallet.Lock();
            TR.Exit();
        }
    }
}
