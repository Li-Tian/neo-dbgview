using System;
using System.IO;
using DbgViewTR;

namespace Neo.Consensus
{
    internal class ChangeView : ConsensusMessage
    {
        public byte NewViewNumber;

        public ChangeView()
            : base(ConsensusMessageType.ChangeView)
        {
            TR.Enter();
            TR.Exit();
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            NewViewNumber = reader.ReadByte(); //读下一个字节
            if (NewViewNumber == 0) throw new FormatException();
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(NewViewNumber);
            TR.Exit();
        }
    }
}
