using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public abstract class BlockBase : IVerifiable
    {
        /// <summary>
        /// 区块版本
        /// </summary>
        public uint Version;
        /// <summary>
        /// 前一个区块的散列值
        /// </summary>
        public UInt256 PrevHash;
        /// <summary>
        /// 该区块中所有交易的Merkle树的根
        /// </summary>
        public UInt256 MerkleRoot;
        /// <summary>
        /// 时间戳
        /// </summary>
        public uint Timestamp;
        /// <summary>
        /// 区块高度
        /// </summary>
        public uint Index;
        public ulong ConsensusData;
        /// <summary>
        /// 下一个区块的记账合约的散列值
        /// </summary>
        public UInt160 NextConsensus;
        /// <summary>
        /// 用于验证该区块的脚本
        /// </summary>
        public Witness Script;

        private UInt256 _hash = null;
        public UInt256 Hash
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

        Witness[] IVerifiable.Scripts
        {
            get
            {
                TR.Log();
                return new[] { Script };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Script = value[0];
            }
        }

        public virtual int Size => sizeof(uint) + PrevHash.Size + MerkleRoot.Size + sizeof(uint) + sizeof(uint) + sizeof(ulong) + NextConsensus.Size + 1 + Script.Size;

        public virtual void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Witness>();
            TR.Exit();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            TR.Enter();
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Index = reader.ReadUInt32();
            ConsensusData = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
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
            if (PrevHash == UInt256.Zero)
                return TR.Exit(new[] { Script.ScriptHash });
            Header prev_header = Blockchain.Default.GetHeader(PrevHash);
            if (prev_header == null) throw new InvalidOperationException();
            return TR.Exit(new UInt160[] { prev_header.NextConsensus });
        }

        public virtual void Serialize(BinaryWriter writer)
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
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(ConsensusData);
            writer.Write(NextConsensus);
            TR.Exit();
        }

        public virtual JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["index"] = Index;
            json["nonce"] = ConsensusData.ToString("x16");
            json["nextconsensus"] = Wallet.ToAddress(NextConsensus);
            json["script"] = Script.ToJson();
            return TR.Exit(json);
        }

        public bool Verify()
        {
            TR.Enter();
            if (Hash == Blockchain.GenesisBlock.Hash) return TR.Exit(true);
            if (Blockchain.Default.ContainsBlock(Hash)) return TR.Exit(true);
            Header prev_header = Blockchain.Default.GetHeader(PrevHash);
            if (prev_header == null) return TR.Exit(false);
            if (prev_header.Index + 1 != Index) return TR.Exit(false);
            if (prev_header.Timestamp >= Timestamp) return TR.Exit(false);
            if (!this.VerifyScripts()) return TR.Exit(false);
            return TR.Exit(true);
        }
    }
}
