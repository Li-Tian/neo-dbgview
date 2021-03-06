﻿using Neo.Core;
using Neo.IO;
using System;
using System.IO;
using System.Linq;
using NoDbgViewTR;

namespace Neo.Consensus
{
    internal class PrepareRequest : ConsensusMessage
    {
        public ulong Nonce;
        public UInt160 NextConsensus;
        public UInt256[] TransactionHashes;
        public MinerTransaction MinerTransaction;
        public byte[] Signature;

        public PrepareRequest()
            : base(ConsensusMessageType.PrepareRequest)
        {
            TR.Enter();
            TR.Exit();
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            Nonce = reader.ReadUInt64();
            NextConsensus = reader.ReadSerializable<UInt160>();
            TransactionHashes = reader.ReadSerializableArray<UInt256>();
            if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
            {
                TR.Exit();
                throw new FormatException();
            }
            MinerTransaction = reader.ReadSerializable<MinerTransaction>();
            if (MinerTransaction.Hash != TransactionHashes[0])
            {
                TR.Exit();
                throw new FormatException();
            }
            Signature = reader.ReadBytes(64);
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(Nonce);
            writer.Write(NextConsensus);
            writer.Write(TransactionHashes);
            writer.Write(MinerTransaction);
            writer.Write(Signature);
            TR.Exit();
        }
    }
}
