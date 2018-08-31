using Neo.IO;
using System.IO;
using NoDbgViewTR;

namespace Neo.Network.Payloads
{
    public class GetBlocksPayload : ISerializable
    {
        public UInt256[] HashStart;
        public UInt256 HashStop;

        public int Size => HashStart.GetVarSize() + HashStop.Size;

        public static GetBlocksPayload Create(UInt256 hash_start, UInt256 hash_stop = null)
        {
            TR.Enter();
            return TR.Exit(new GetBlocksPayload
            {
                HashStart = new[] { hash_start },
                HashStop = hash_stop ?? UInt256.Zero
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            HashStart = reader.ReadSerializableArray<UInt256>(16);
            HashStop = reader.ReadSerializable<UInt256>();
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(HashStart);
            writer.Write(HashStop);
            TR.Exit();
        }
    }
}
