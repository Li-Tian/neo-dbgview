using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Core
{
    public class StateTransaction : Transaction
    {
        public StateDescriptor[] Descriptors;

        public override int Size => base.Size + Descriptors.GetVarSize();
        public override Fixed8 SystemFee => Descriptors.Sum(p => p.SystemFee);

        public StateTransaction()
            : base(TransactionType.StateTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            TR.Enter();
            Descriptors = reader.ReadSerializableArray<StateDescriptor>(16);
            TR.Exit();
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            TR.Enter();
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (StateDescriptor descriptor in Descriptors)
            {
                switch (descriptor.Type)
                {
                    case StateType.Account:
                        hashes.UnionWith(GetScriptHashesForVerifying_Account(descriptor));
                        break;
                    case StateType.Validator:
                        hashes.UnionWith(GetScriptHashesForVerifying_Validator(descriptor));
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            return TR.Exit(hashes.OrderBy(p => p).ToArray());
        }

        private IEnumerable<UInt160> GetScriptHashesForVerifying_Account(StateDescriptor descriptor)
        {
            TR.Enter();
            switch (descriptor.Field)
            {
                case "Votes":
                    yield return TR.Exit(new UInt160(descriptor.Key));
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private IEnumerable<UInt160> GetScriptHashesForVerifying_Validator(StateDescriptor descriptor)
        {
            TR.Enter();
            switch (descriptor.Field)
            {
                case "Registered":
                    yield return TR.Exit(Contract.CreateSignatureRedeemScript(ECPoint.DecodePoint(descriptor.Key, ECCurve.Secp256r1)).ToScriptHash());
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(Descriptors);
            TR.Exit();
        }

        public override JObject ToJson()
        {
            TR.Enter();
            JObject json = base.ToJson();
            json["descriptors"] = new JArray(Descriptors.Select(p => p.ToJson()));
            return TR.Exit(json);
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            TR.Enter();
            foreach (StateDescriptor descriptor in Descriptors)
                if (!descriptor.Verify())
                    return TR.Exit(false);
            return TR.Exit(base.Verify(mempool));
        }
    }
}
