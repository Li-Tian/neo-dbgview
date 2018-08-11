using System.IO;
using DbgViewTR;

namespace Neo.Consensus
{
    internal class PrepareResponse : ConsensusMessage
    {
        public byte[] Signature;

        public PrepareResponse()
            : base(ConsensusMessageType.PrepareResponse)
        {
            TR.Enter();
            TR.Exit();
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            Signature = reader.ReadBytes(64);
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(Signature);
            TR.Exit();
        }
    }
}
