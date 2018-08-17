using Neo.IO;
using System.IO;
using DbgViewTR;

namespace Neo.Network.Payloads
{
    public class FilterAddPayload : ISerializable
    {
        public byte[] Data;

        public int Size => Data.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Data = reader.ReadVarBytes(520);
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.WriteVarBytes(Data);
            TR.Exit();
        }
    }
}
