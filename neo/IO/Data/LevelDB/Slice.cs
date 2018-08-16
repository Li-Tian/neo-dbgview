using Neo.Cryptography;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public struct Slice : IComparable<Slice>, IEquatable<Slice>
    {
        internal byte[] buffer;

        internal Slice(IntPtr data, UIntPtr length)
        {
            TR.Enter();
            buffer = new byte[(int)length];
            Marshal.Copy(data, buffer, 0, (int)length);
            TR.Exit();
        }

        public int CompareTo(Slice other)
        {
            TR.Enter();
            for (int i = 0; i < buffer.Length && i < other.buffer.Length; i++)
            {
                int r = buffer[i].CompareTo(other.buffer[i]);
                if (r != 0) return TR.Exit(r);
            }
            return TR.Exit(buffer.Length.CompareTo(other.buffer.Length));
        }

        public bool Equals(Slice other)
        {
            TR.Enter();
            if (buffer.Length != other.buffer.Length) return TR.Exit(false);
            return TR.Exit(buffer.SequenceEqual(other.buffer));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            if (ReferenceEquals(null, obj)) return TR.Exit(false);
            if (!(obj is Slice)) return TR.Exit(false);
            return TR.Exit(Equals((Slice)obj));
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit((int)buffer.Murmur32(0));
        }

        public byte[] ToArray()
        {
            TR.Enter();
            return TR.Exit(buffer ?? new byte[0]);
        }

        unsafe public bool ToBoolean()
        {
            TR.Enter();
            if (buffer.Length != sizeof(bool))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((bool*)pbyte));
            }
        }

        public byte ToByte()
        {
            TR.Enter();
            if (buffer.Length != sizeof(byte))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            return TR.Exit(buffer[0]);
        }

        unsafe public double ToDouble()
        {
            if (buffer.Length != sizeof(double))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((double*)pbyte));
            }
        }

        unsafe public short ToInt16()
        {
            TR.Enter();
            if (buffer.Length != sizeof(short))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((short*)pbyte));
            }
        }

        unsafe public int ToInt32()
        {
            TR.Enter();
            if (buffer.Length != sizeof(int))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((int*)pbyte));
            }
        }

        unsafe public long ToInt64()
        {
            TR.Enter();
            if (buffer.Length != sizeof(long))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((long*)pbyte));
            }
        }

        unsafe public float ToSingle()
        {
            TR.Enter();
            if (buffer.Length != sizeof(float))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((float*)pbyte));
            }
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(Encoding.UTF8.GetString(buffer));
        }

        unsafe public ushort ToUInt16()
        {
            TR.Enter();
            if (buffer.Length != sizeof(ushort))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((ushort*)pbyte));
            }
        }

        unsafe public uint ToUInt32(int index = 0)
        {
            TR.Enter();
            if (buffer.Length != sizeof(uint) + index)
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[index])
            {
                return TR.Exit(*((uint*)pbyte));
            }
        }

        unsafe public ulong ToUInt64()
        {
            if (buffer.Length != sizeof(ulong))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            fixed (byte* pbyte = &buffer[0])
            {
                return TR.Exit(*((ulong*)pbyte));
            }
        }

        public static implicit operator Slice(byte[] data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = data });
        }

        public static implicit operator Slice(bool data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(byte data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = new[] { data } });
        }

        public static implicit operator Slice(double data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(short data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(int data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(long data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(float data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(string data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = Encoding.UTF8.GetBytes(data) });
        }

        public static implicit operator Slice(ushort data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(uint data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static implicit operator Slice(ulong data)
        {
            TR.Enter();
            return TR.Exit(new Slice { buffer = BitConverter.GetBytes(data) });
        }

        public static bool operator <(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) < 0);
        }

        public static bool operator <=(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) <= 0);
        }

        public static bool operator >(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) > 0);
        }

        public static bool operator >=(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) >= 0);
        }

        public static bool operator ==(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(x.Equals(y));
        }

        public static bool operator !=(Slice x, Slice y)
        {
            TR.Enter();
            return TR.Exit(!x.Equals(y));
        }
    }
}
