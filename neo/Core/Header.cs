using Neo.IO;
using Neo.VM;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public class Header : BlockBase, IEquatable<Header>
    {
        public override int Size => base.Size + 1;

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            if (reader.ReadByte() != 0) throw new FormatException();
            TR.Exit();
        }

        public bool Equals(Header other)
        {
            TR.Enter();
            if (ReferenceEquals(other, null)) return TR.Exit(false);
            if (ReferenceEquals(other, this)) return TR.Exit(true);
            return TR.Exit(Hash.Equals(other.Hash));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            return TR.Exit(Equals(obj as Header));
        }

        public static Header FromTrimmedData(byte[] data, int index)
        {
            TR.Enter();
            Header header = new Header();
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((IVerifiable)header).DeserializeUnsigned(reader);
                reader.ReadByte(); header.Script = reader.ReadSerializable<Witness>();
            }
            return TR.Exit(header);
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(Hash.GetHashCode());
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write((byte)0);
            TR.Exit();
        }
    }
}
