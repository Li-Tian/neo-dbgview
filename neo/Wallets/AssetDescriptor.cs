﻿using Neo.Core;
using Neo.SmartContract;
using Neo.VM;
using System;
using DbgViewTR;

namespace Neo.Wallets
{
    public class AssetDescriptor
    {
        public UIntBase AssetId;
        public string AssetName;
        public byte Decimals;

        public AssetDescriptor(UIntBase asset_id)
        {
            TR.Enter();
            if (asset_id is UInt160 asset_id_160)
            {
                byte[] script;
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    sb.EmitAppCall(asset_id_160, "decimals");
                    sb.EmitAppCall(asset_id_160, "name");
                    script = sb.ToArray();
                }
                ApplicationEngine engine = ApplicationEngine.Run(script);
                if (engine.State.HasFlag(VMState.FAULT))
                {
                    TR.Exit();
                    throw new ArgumentException();
                }
                this.AssetId = asset_id;
                this.AssetName = engine.EvaluationStack.Pop().GetString();
                this.Decimals = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            }
            else
            {
                AssetState state = Blockchain.Default.GetAssetState((UInt256)asset_id);
                this.AssetId = state.AssetId;
                this.AssetName = state.GetName();
                this.Decimals = state.Precision;
            }
            TR.Exit();
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(AssetName);
        }
    }
}
