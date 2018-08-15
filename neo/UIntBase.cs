using NoDbgViewTR;
using Neo.IO;
using System;
using System.IO;
using System.Linq;

namespace Neo
{
    public abstract class UIntBase : IEquatable<UIntBase>, ISerializable
    {
        private byte[] data_bytes;

        public int Size => data_bytes.Length;

        protected UIntBase(int bytes, byte[] value)
        {
            TR.Enter();
            if (value == null)
            {
                this.data_bytes = new byte[bytes];
                TR.Exit();
                return;
            }
            if (value.Length != bytes)
                throw new ArgumentException();
            this.data_bytes = value;
            TR.Exit();
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            reader.Read(data_bytes, 0, data_bytes.Length);
            TR.Exit();
        }

        public bool Equals(UIntBase other)
        {
            TR.Enter();
            if (ReferenceEquals(other, null))
                return TR.Exit(false);
            if (ReferenceEquals(this, other))
                return TR.Exit(true);
            if (data_bytes.Length != other.data_bytes.Length)
                return TR.Exit(false);
            return TR.Exit(data_bytes.SequenceEqual(other.data_bytes));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            if (ReferenceEquals(obj, null))
                return TR.Exit(false);
            if (!(obj is UIntBase))
                return TR.Exit(false);
            return TR.Exit(this.Equals((UIntBase)obj));
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(data_bytes.ToInt32(0));
        }

        public static UIntBase Parse(string s)
        {
            TR.Enter();
            if (s.Length == 40 || s.Length == 42)
                return TR.Exit(UInt160.Parse(s));
            else if (s.Length == 64 || s.Length == 66)
                return TR.Exit(UInt256.Parse(s));
            else
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(data_bytes);
            TR.Exit();
        }

        public byte[] ToArray()
        {
            TR.Enter();
            return TR.Exit(data_bytes);
        }

        /// <summary>
        /// 转为16进制字符串
        /// </summary>
        /// <returns>返回16进制字符串</returns>
        public override string ToString()
        {
            TR.Enter();
            return TR.Exit("0x" + data_bytes.Reverse().ToHexString());
        }

        public static bool TryParse<T>(string s, out T result) where T : UIntBase
        {
            TR.Enter();
            int size;
            if (typeof(T) == typeof(UInt160))
                size = 20;
            else if (typeof(T) == typeof(UInt256))
                size = 32;
            else if (s.Length == 40 || s.Length == 42)
                size = 20;
            else if (s.Length == 64 || s.Length == 66)
                size = 32;
            else
                size = 0;
            if (size == 20)
            {
                if (UInt160.TryParse(s, out UInt160 r))
                {
                    result = (T)(UIntBase)r;
                    return TR.Exit(true);
                }
            }
            else if (size == 32)
            {
                if (UInt256.TryParse(s, out UInt256 r))
                {
                    result = (T)(UIntBase)r;
                    return TR.Exit(true);
                }
            }
            result = null;
            return TR.Exit(false);
        }

        public static bool operator ==(UIntBase left, UIntBase right)
        {
            TR.Enter();
            if (ReferenceEquals(left, right))
                return TR.Exit(true);
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return TR.Exit(false);
            return TR.Exit(left.Equals(right));
        }

        public static bool operator !=(UIntBase left, UIntBase right)
        {
            TR.Enter();
            return TR.Exit(!(left == right));
        }
    }
}
