using System;
using System.Globalization;
using System.Linq;
using DbgViewTR;

namespace Neo
{
    public class UInt256 : UIntBase, IComparable<UInt256>, IEquatable<UInt256>
    {
        public static readonly UInt256 Zero = new UInt256();

        public UInt256()
            : this(null)
        {
            TR.Log();
        }

        public UInt256(byte[] value)
            : base(32, value)
        {
            TR.Log();
        }

        public int CompareTo(UInt256 other)
        {
            TR.Enter();
            byte[] x = ToArray();
            byte[] y = other.ToArray();
            for (int i = x.Length - 1; i >= 0; i--)
            {
                if (x[i] > y[i])
                    return TR.Exit(1);
                if (x[i] < y[i])
                    return TR.Exit(-1);
            }
            return TR.Exit(0);
        }

        bool IEquatable<UInt256>.Equals(UInt256 other)
        {
            TR.Enter();
            return TR.Exit(Equals(other));
        }

        public static new UInt256 Parse(string s)
        {
            TR.Enter();
            if (s == null)
                throw new ArgumentNullException();
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 64)
                throw new FormatException();
            return TR.Exit(new UInt256(s.HexToBytes().Reverse().ToArray()));
        }

        public static bool TryParse(string s, out UInt256 result)
        {
            TR.Enter();
            if (s == null)
            {
                result = null;
                return TR.Exit(false);
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 64)
            {
                result = null;
                return TR.Exit(false);
            }
            byte[] data = new byte[32];
            for (int i = 0; i < 32; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return TR.Exit(false);
                }
            result = new UInt256(data.Reverse().ToArray());
            return TR.Exit(true);
        }

        public static bool operator >(UInt256 left, UInt256 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) > 0);
        }

        public static bool operator >=(UInt256 left, UInt256 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) >= 0);
        }

        public static bool operator <(UInt256 left, UInt256 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) < 0);
        }

        public static bool operator <=(UInt256 left, UInt256 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) <= 0);
        }
    }
}
