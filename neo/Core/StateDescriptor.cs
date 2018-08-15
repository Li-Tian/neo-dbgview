using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Core
{
    public class StateDescriptor : ISerializable
    {
        public StateType Type;
        public byte[] Key;
        public string Field;
        public byte[] Value;

        public int Size => sizeof(StateType) + Key.GetVarSize() + Field.GetVarSize() + Value.GetVarSize();

        public Fixed8 SystemFee
        {
            get
            {
                switch (Type)
                {
                    case StateType.Validator:
                        return GetSystemFee_Validator();
                    default:
                        return Fixed8.Zero;
                }
            }
        }

        private void CheckAccountState()
        {
            TR.Enter();
            if (Key.Length != 20) throw new FormatException();
            if (Field != "Votes") throw new FormatException();
            TR.Exit();
        }

        private void CheckValidatorState()
        {
            TR.Enter();
            if (Key.Length != 33) throw new FormatException();
            if (Field != "Registered") throw new FormatException();
            TR.Exit();
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Type = (StateType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(StateType), Type))
                throw new FormatException();
            Key = reader.ReadVarBytes(100);
            Field = reader.ReadVarString(32);
            Value = reader.ReadVarBytes(65535);
            switch (Type)
            {
                case StateType.Account:
                    CheckAccountState();
                    break;
                case StateType.Validator:
                    CheckValidatorState();
                    break;
            }
            TR.Exit();
        }

        private Fixed8 GetSystemFee_Validator()
        {
            TR.Enter();
            switch (Field)
            {
                case "Registered":
                    if (Value.Any(p => p != 0))
                        return TR.Exit(Fixed8.FromDecimal(1000));
                    else
                        return TR.Exit(Fixed8.Zero);
                default:
                    throw new InvalidOperationException();
            }
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write((byte)Type);
            writer.WriteVarBytes(Key);
            writer.WriteVarString(Field);
            writer.WriteVarBytes(Value);
            TR.Exit();
        }

        public JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["type"] = Type;
            json["key"] = Key.ToHexString();
            json["field"] = Field;
            json["value"] = Value.ToHexString();
            return TR.Exit(json);
        }

        internal bool Verify()
        {
            TR.Enter();
            switch (Type)
            {
                case StateType.Account:
                    return TR.Exit(VerifyAccountState());
                case StateType.Validator:
                    return TR.Exit(VerifyValidatorState());
                default:
                    return TR.Exit(false);
            }
        }

        private bool VerifyAccountState()
        {
            TR.Enter();
            switch (Field)
            {
                case "Votes":
                    if (Blockchain.Default == null) return TR.Exit(false);
                    ECPoint[] pubkeys;
                    try
                    {
                        pubkeys = Value.AsSerializableArray<ECPoint>((int)Blockchain.MaxValidators);
                    }
                    catch (FormatException)
                    {
                        return TR.Exit(false);
                    }
                    UInt160 hash = new UInt160(Key);
                    AccountState account = Blockchain.Default.GetAccountState(hash);
                    if (account?.IsFrozen != false) return TR.Exit(false);
                    if (pubkeys.Length > 0)
                    {
                        if (account.GetBalance(Blockchain.GoverningToken.Hash).Equals(Fixed8.Zero)) return TR.Exit(false);
                        HashSet<ECPoint> sv = new HashSet<ECPoint>(Blockchain.StandbyValidators);
                        DataCache<ECPoint, ValidatorState> validators = Blockchain.Default.GetStates<ECPoint, ValidatorState>();
                        foreach (ECPoint pubkey in pubkeys)
                            if (!sv.Contains(pubkey) && validators.TryGet(pubkey)?.Registered != true)
                                return TR.Exit(false);
                    }
                    return TR.Exit(true);
                default:
                    return TR.Exit(false);
            }
        }

        private bool VerifyValidatorState()
        {
            TR.Enter();
            switch (Field)
            {
                case "Registered":
                    return TR.Exit(true);
                default:
                    return TR.Exit(false);
            }
        }
    }
}
