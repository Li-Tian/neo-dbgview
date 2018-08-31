using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public abstract class StateBase : IInteropInterface, ISerializable
    {
        public const byte StateVersion = 0;

        public virtual int Size => sizeof(byte);

        public virtual void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            if (reader.ReadByte() != StateVersion) throw new FormatException();
            TR.Exit();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(StateVersion);
            TR.Exit();
        }

        public virtual JObject ToJson()
        {
            TR.Enter();
            JObject json = new JObject();
            json["version"] = StateVersion;
            return TR.Exit(json);
        }
    }
}
