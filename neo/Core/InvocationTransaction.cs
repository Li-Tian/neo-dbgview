using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public class InvocationTransaction : Transaction
    {
        public byte[] Script;
        public Fixed8 Gas;

        public override int Size => base.Size + Script.GetVarSize();

        public override Fixed8 SystemFee => Gas;

        public InvocationTransaction()
            : base(TransactionType.InvocationTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            TR.Enter();
            if (Version > 1) throw new FormatException();
            Script = reader.ReadVarBytes(65536);
            if (Script.Length == 0) throw new FormatException();
            if (Version >= 1)
            {
                Gas = reader.ReadSerializable<Fixed8>();
                if (Gas < Fixed8.Zero) throw new FormatException();
            }
            else
            {
                Gas = Fixed8.Zero;
            }
            TR.Exit();
        }

        public static Fixed8 GetGas(Fixed8 consumed)
        {
            TR.Enter();
            Fixed8 gas = consumed - Fixed8.FromDecimal(10);
            if (gas <= Fixed8.Zero) return TR.Exit(Fixed8.Zero);
            return TR.Exit(gas.Ceiling());
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            TR.Enter();
            writer.WriteVarBytes(Script);
            if (Version >= 1)
                writer.Write(Gas);
            TR.Exit();
        }

        public override JObject ToJson()
        {
            TR.Enter();
            JObject json = base.ToJson();
            json["script"] = Script.ToHexString();
            json["gas"] = Gas.ToString();
            return TR.Exit(json);
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            TR.Enter();
            if (Gas.GetData() % 100000000 != 0) return TR.Exit(false);
            return TR.Exit(base.Verify(mempool));
        }
    }
}
