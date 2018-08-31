using Neo.IO;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Network.Payloads
{
    public class InvPayload : ISerializable
    {
        public InventoryType Type;
        public UInt256[] Hashes;

        public int Size => sizeof(InventoryType) + Hashes.GetVarSize();

        public static InvPayload Create(InventoryType type, params UInt256[] hashes)
        {
            TR.Enter();
            return TR.Exit(new InvPayload
            {
                Type = type,
                Hashes = hashes
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Type = (InventoryType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(InventoryType), Type))
            {
                TR.Exit();
                throw new FormatException();
            }
            Hashes = reader.ReadSerializableArray<UInt256>();
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write((byte)Type);
            writer.Write(Hashes);
            TR.Exit();
        }
    }
}
