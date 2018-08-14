﻿using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Core
{
    public class AccountState : StateBase, ICloneable<AccountState>
    {
        public UInt160 ScriptHash;
        public bool IsFrozen;
        public ECPoint[] Votes;
        public Dictionary<UInt256, Fixed8> Balances;

        public override int Size => base.Size + ScriptHash.Size + sizeof(bool) + Votes.GetVarSize()
            + IO.Helper.GetVarSize(Balances.Count) + Balances.Count * (32 + 8);

        public AccountState() { }

        public AccountState(UInt160 hash)
        {
            TR.Enter();
            this.ScriptHash = hash;
            this.IsFrozen = false;
            this.Votes = new ECPoint[0];
            this.Balances = new Dictionary<UInt256, Fixed8>();
            TR.Exit();
        }

        AccountState ICloneable<AccountState>.Clone()
        {
            TR.Enter();
            return TR.Exit(new AccountState
            {
                ScriptHash = ScriptHash,
                IsFrozen = IsFrozen,
                Votes = Votes,
                Balances = Balances.ToDictionary(p => p.Key, p => p.Value)
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            ScriptHash = reader.ReadSerializable<UInt160>();
            IsFrozen = reader.ReadBoolean();
            Votes = new ECPoint[reader.ReadVarInt()];
            for (int i = 0; i < Votes.Length; i++)
                Votes[i] = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            int count = (int)reader.ReadVarInt();
            Balances = new Dictionary<UInt256, Fixed8>(count);
            for (int i = 0; i < count; i++)
            {
                UInt256 assetId = reader.ReadSerializable<UInt256>();
                Fixed8 value = reader.ReadSerializable<Fixed8>();
                Balances.Add(assetId, value);
            }
            TR.Exit();
        }

        void ICloneable<AccountState>.FromReplica(AccountState replica)
        {
            TR.Enter();
            ScriptHash = replica.ScriptHash;
            IsFrozen = replica.IsFrozen;
            Votes = replica.Votes;
            Balances = replica.Balances;
            TR.Exit();
        }

        public Fixed8 GetBalance(UInt256 asset_id)
        {
            TR.Enter();
            if (!Balances.TryGetValue(asset_id, out Fixed8 value))
                value = Fixed8.Zero;
            return TR.Exit(value);
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(ScriptHash);
            writer.Write(IsFrozen);
            writer.Write(Votes);
            var balances = Balances.Where(p => p.Value > Fixed8.Zero).ToArray();
            writer.WriteVarInt(balances.Length);
            foreach (var pair in balances)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
            TR.Exit();
        }

        public override JObject ToJson()
        {
            TR.Enter();
            JObject json = base.ToJson();
            json["script_hash"] = ScriptHash.ToString();
            json["frozen"] = IsFrozen;
            json["votes"] = new JArray(Votes.Select(p => (JObject)p.ToString()));
            json["balances"] = new JArray(Balances.Select(p =>
            {
                JObject balance = new JObject();
                balance["asset"] = p.Key.ToString();
                balance["value"] = p.Value.ToString();
                return TR.Exit(balance);
            }));
            return TR.Exit(json);
        }
    }
}
