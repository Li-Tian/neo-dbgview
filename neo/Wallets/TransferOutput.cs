using Neo.Core;
using System;
using NoDbgViewTR;

namespace Neo.Wallets
{
    public class TransferOutput
    {
        public UIntBase AssetId;
        public BigDecimal Value;
        public UInt160 ScriptHash;

        public bool IsGlobalAsset => AssetId.Size == 32;

        public TransactionOutput ToTxOutput()
        {
            TR.Enter();
            if (AssetId is UInt256 asset_id)
                return TR.Exit(new TransactionOutput
                {
                    AssetId = asset_id,
                    Value = Value.ToFixed8(),
                    ScriptHash = ScriptHash
                });
            TR.Exit();
            throw new NotSupportedException();
        }
    }
}
