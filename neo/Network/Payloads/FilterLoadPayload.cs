using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Network.Payloads
{
    public class FilterLoadPayload : ISerializable
    {
        public byte[] Filter;
        public byte K;
        public uint Tweak;

        public int Size => Filter.GetVarSize() + sizeof(byte) + sizeof(uint);

        public static FilterLoadPayload Create(BloomFilter filter)
        {
            TR.Enter();
            byte[] buffer = new byte[filter.M / 8];
            filter.GetBits(buffer);
            return TR.Exit(new FilterLoadPayload
            {
                Filter = buffer,
                K = (byte)filter.K,
                Tweak = filter.Tweak
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Filter = reader.ReadVarBytes(36000);
            K = reader.ReadByte();
            if (K > 50)
            {
                TR.Exit();
                throw new FormatException();
            }
            Tweak = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.WriteVarBytes(Filter);
            writer.Write(K);
            writer.Write(Tweak);
            TR.Exit();
        }
    }
}
