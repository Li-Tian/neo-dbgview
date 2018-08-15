﻿using Neo.IO;
using System.Collections.Generic;
using System.IO;
using DbgViewTR;

namespace Neo.Core
{
    public class SpentCoinState : StateBase, ICloneable<SpentCoinState>
    {
        public UInt256 TransactionHash;
        public uint TransactionHeight;
        public Dictionary<ushort, uint> Items;

        public override int Size => base.Size + TransactionHash.Size + sizeof(uint)
            + IO.Helper.GetVarSize(Items.Count) + Items.Count * (sizeof(ushort) + sizeof(uint));

        SpentCoinState ICloneable<SpentCoinState>.Clone()
        {
            TR.Enter();
            return TR.Exit(new SpentCoinState
            {
                TransactionHash = TransactionHash,
                TransactionHeight = TransactionHeight,
                Items = new Dictionary<ushort, uint>(Items)
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            TransactionHash = reader.ReadSerializable<UInt256>();
            TransactionHeight = reader.ReadUInt32();
            int count = (int)reader.ReadVarInt();
            Items = new Dictionary<ushort, uint>(count);
            for (int i = 0; i < count; i++)
            {
                ushort index = reader.ReadUInt16();
                uint height = reader.ReadUInt32();
                Items.Add(index, height);
            }
            TR.Exit();
        }

        void ICloneable<SpentCoinState>.FromReplica(SpentCoinState replica)
        {
            TR.Enter();
            TransactionHash = replica.TransactionHash;
            TransactionHeight = replica.TransactionHeight;
            Items = replica.Items;
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(TransactionHash);
            writer.Write(TransactionHeight);
            writer.WriteVarInt(Items.Count);
            foreach (var pair in Items)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
            TR.Exit();
        }
    }
}
