using System.IO;
using NoDbgViewTR;

namespace Neo.IO.Wrappers
{
    internal class ByteWrapper : SerializableWrapper<byte>
    {
        private byte value;

        public override int Size => sizeof(byte);

        public ByteWrapper(byte value)
        {
            TR.Enter();
            this.value = value;
            TR.Exit();
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            value = reader.ReadByte();
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(value);
            TR.Exit();
        }
    }
}
