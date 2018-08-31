using Neo.IO;
using Neo.IO.Caching;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Consensus
{
    internal abstract class ConsensusMessage : ISerializable
    {
        /// <summary>
        /// Reflection cache for ConsensusMessageType
        /// </summary>
        private static ReflectionCache<byte> ReflectionCache = ReflectionCache<byte>.CreateFromEnum<ConsensusMessageType>();

        public readonly ConsensusMessageType Type;
        public byte ViewNumber;

        //Size equals to 33? 
        public int Size => sizeof(ConsensusMessageType) + sizeof(byte);


        protected ConsensusMessage(ConsensusMessageType type)
        {
            TR.Enter();
            this.Type = type;
            TR.Exit();
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            if (Type != (ConsensusMessageType)reader.ReadByte())
                throw new FormatException();
            ViewNumber = reader.ReadByte();
            TR.Exit();
        }

        public static ConsensusMessage DeserializeFrom(byte[] data)
        {
            TR.Enter();
            ConsensusMessage message = ReflectionCache.CreateInstance<ConsensusMessage>(data[0]);
            if (message == null) throw new FormatException();

            using (MemoryStream ms = new MemoryStream(data, false)) //MemoryStream和BinaryReader的
            using (BinaryReader r = new BinaryReader(ms))
            {
                message.Deserialize(r); //only readsin 1 byte
            }
            return TR.Exit(message);
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write((byte)Type);
            writer.Write(ViewNumber);
            TR.Exit();
        }
    }
}
