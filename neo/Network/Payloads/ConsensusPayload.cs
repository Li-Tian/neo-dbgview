using Neo.Core;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;
using DbgViewTR;

namespace Neo.Network.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public ushort ValidatorIndex;
        public uint Timestamp;
        public byte[] Data;
        public Witness Script;

        private UInt256 _hash = null;
        UInt256 IInventory.Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

        Witness[] IVerifiable.Scripts
        {
            get
            {
                return new[] { Script };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Script = value[0];
                TR.Log("{0}", Script.GetType().ToString());
                TR.Log("{0}", value[0].GetType().ToString());
                //FS: why value[0]，and what is this value? 保持接口统一？

            }
        }

        public int Size => sizeof(uint) + PrevHash.Size + sizeof(uint) + sizeof(ushort) + sizeof(uint) + Data.GetVarSize() + 1 + Script.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1)
            {
                TR.Exit();
                throw new FormatException();
            }
            Script = reader.ReadSerializable<Witness>();
            TR.Exit();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            TR.Enter();
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadUInt16();
            Timestamp = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
            TR.Exit();
        }

        byte[] IScriptContainer.GetMessage()
        {
            TR.Enter();
            return TR.Exit(this.GetHashData());
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying()
        {
            TR.Enter();
            if (Blockchain.Default == null)
            {
                TR.Exit();
                throw new InvalidOperationException();
            }
            ECPoint[] validators = Blockchain.Default.GetValidators();
            if (validators.Length <= ValidatorIndex)
            {
                TR.Exit();
                throw new InvalidOperationException();
            }
            return TR.Exit(new[] { Contract.CreateSignatureRedeemScript(validators[ValidatorIndex]).ToScriptHash() });
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
            TR.Exit();
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.Write(Timestamp);
            writer.WriteVarBytes(Data);
            TR.Exit();
        }

        public bool Verify()
        {
            TR.Enter();
            if (Blockchain.Default == null) return TR.Exit(false);
            if (BlockIndex <= Blockchain.Default.Height)
                return TR.Exit(false);
            return TR.Exit(this.VerifyScripts());
        }
    }
}
