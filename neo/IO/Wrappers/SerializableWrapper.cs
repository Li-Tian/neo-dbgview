using System;
using System.IO;
using NoDbgViewTR;

namespace Neo.IO.Wrappers
{
    public abstract class SerializableWrapper : ISerializable
    {
        public abstract int Size { get; }

        public abstract void Deserialize(BinaryReader reader);
        public abstract void Serialize(BinaryWriter writer);

        public static implicit operator SerializableWrapper(byte value)
        {
            TR.Enter();
            return TR.Exit(new ByteWrapper(value));
        }
    }

    public abstract class SerializableWrapper<T> : SerializableWrapper where T : IEquatable<T>
    {
    }
}
