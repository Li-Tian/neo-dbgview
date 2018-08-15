using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Core
{
    public class StorageKey : IEquatable<StorageKey>, ISerializable
    {
        public UInt160 ScriptHash;
        public byte[] Key;

        int ISerializable.Size => ScriptHash.Size + (Key.Length / 16 + 1) * 17;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            ScriptHash = reader.ReadSerializable<UInt160>();
            Key = reader.ReadBytesWithGrouping();
            TR.Exit();
        }

        public bool Equals(StorageKey other)
        {
            TR.Enter();
            if (ReferenceEquals(other, null))
                return TR.Exit(false);
            if (ReferenceEquals(this, other))
                return TR.Exit(true);
            return TR.Exit(ScriptHash.Equals(other.ScriptHash) && Key.SequenceEqual(other.Key));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            if (ReferenceEquals(obj, null)) return TR.Exit(false);
            if (!(obj is StorageKey)) return TR.Exit(false);
            return TR.Exit(Equals((StorageKey)obj));
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(ScriptHash.GetHashCode() + (int)Key.Murmur32(0));
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(ScriptHash);
            writer.WriteBytesWithGrouping(Key);
            TR.Exit();
        }
    }
}
