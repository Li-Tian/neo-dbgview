using System;
using System.Globalization;
using System.Linq;
using DbgViewTR;

namespace Neo
{
    public class UInt160 : UIntBase, IComparable<UInt160>, IEquatable<UInt160>
    {
        public static readonly UInt160 Zero = new UInt160();

        public UInt160()
            : this(null)
        {
        }

        public UInt160(byte[] value)
            : base(20, value)
        {
        }

        public int CompareTo(UInt160 other)
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

        bool IEquatable<UInt160>.Equals(UInt160 other)
        {
            TR.Enter();
            return TR.Exit(Equals(other));
        }

        public static new UInt160 Parse(string value)
        {
            TR.Enter();
            if (value == null)
                throw new ArgumentNullException();
            if (value.StartsWith("0x"))
                value = value.Substring(2);
            if (value.Length != 40)
                throw new FormatException();
            return TR.Exit(new UInt160(value.HexToBytes().Reverse().ToArray()));
        }

        public static bool TryParse(string s, out UInt160 result)
        {
            TR.Enter();
            if (s == null)
            {
                result = null;
                return TR.Exit(false);
            }
            if (s.StartsWith("0x"))
                s = s.Substring(2);
            if (s.Length != 40)
            {
                result = null;
                return TR.Exit(false);
            }
            byte[] data = new byte[20];
            for (int i = 0; i < 20; i++)
                if (!byte.TryParse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, null, out data[i]))
                {
                    result = null;
                    return TR.Exit(false);
                }
            result = new UInt160(data.Reverse().ToArray());
            return TR.Exit(true);
        }

        public static bool operator >(UInt160 left, UInt160 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) > 0);
        }

        public static bool operator >=(UInt160 left, UInt160 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) >= 0);
        }

        public static bool operator <(UInt160 left, UInt160 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) < 0);
        }

        public static bool operator <=(UInt160 left, UInt160 right)
        {
            TR.Enter();
            return TR.Exit(left.CompareTo(right) <= 0);
        }
    }
}
