using Neo.IO;
using System;
using System.Globalization;
using System.IO;
using DbgViewTR;

namespace Neo
{
    /// <summary>
    /// Accurate to 10^-8 64-bit fixed-point numbers minimize rounding errors.
    /// By controlling the accuracy of the multiplier, rounding errors can be completely eliminated.
    /// </summary>
    public struct Fixed8 : IComparable<Fixed8>, IEquatable<Fixed8>, IFormattable, ISerializable
    {
        private const long D = 100_000_000;
        internal long value;

        public static readonly Fixed8 MaxValue = new Fixed8 { value = long.MaxValue };

        public static readonly Fixed8 MinValue = new Fixed8 { value = long.MinValue };

        public static readonly Fixed8 One = new Fixed8 { value = D };

        public static readonly Fixed8 Satoshi = new Fixed8 { value = 1 };

        public static readonly Fixed8 Zero = default(Fixed8);

        public int Size => sizeof(long);

        public Fixed8(long data)
        {
            TR.Enter();
            this.value = data;
            TR.Exit();
        }

        public Fixed8 Abs()
        {
            TR.Enter();
            if (value >= 0) return TR.Exit(this);
            return TR.Exit(new Fixed8
            {
                value = -value
            });
        }

        public Fixed8 Ceiling()
        {
            TR.Enter();
            long remainder = value % D;
            if (remainder == 0) return TR.Exit(this);
            if (remainder > 0)
                return TR.Exit(new Fixed8
                {
                    value = value - remainder + D
                });
            else
                return TR.Exit(new Fixed8
                {
                    value = value - remainder
                });
        }

        public int CompareTo(Fixed8 other)
        {
            TR.Enter();
            return TR.Exit(value.CompareTo(other.value));
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            value = reader.ReadInt64();
            TR.Exit();
        }

        public bool Equals(Fixed8 other)
        {
            TR.Enter();
            return TR.Exit(value.Equals(other.value));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            if (!(obj is Fixed8)) return TR.Exit(false);
            return TR.Exit(Equals((Fixed8)obj));
        }

        public static Fixed8 FromDecimal(decimal value)
        {
            TR.Enter();
            value *= D;
            if (value < long.MinValue || value > long.MaxValue)
                throw new OverflowException();
            return TR.Exit(new Fixed8
            {
                value = (long)value
            });
        }

        public long GetData() => value;

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(value.GetHashCode());
        }

        public static Fixed8 Max(Fixed8 first, params Fixed8[] others)
        {
            TR.Enter();
            foreach (Fixed8 other in others)
            {
                if (first.CompareTo(other) < 0)
                    first = other;
            }
            return TR.Exit(first);
        }

        public static Fixed8 Min(Fixed8 first, params Fixed8[] others)
        {
            TR.Enter();
            foreach (Fixed8 other in others)
            {
                if (first.CompareTo(other) > 0)
                    first = other;
            }
            return TR.Exit(first);
        }

        public static Fixed8 Parse(string s)
        {
            TR.Enter();
            return TR.Exit(FromDecimal(decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)));
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(value);
            TR.Exit();
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(((decimal)this).ToString(CultureInfo.InvariantCulture));
        }

        public string ToString(string format)
        {
            TR.Enter();
            return TR.Exit(((decimal)this).ToString(format));
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            TR.Enter();
            return TR.Exit(((decimal)this).ToString(format, formatProvider));
        }

        public static bool TryParse(string s, out Fixed8 result)
        {
            TR.Enter();
            decimal d;
            if (!decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                result = default(Fixed8);
                return TR.Exit(false);
            }
            d *= D;
            if (d < long.MinValue || d > long.MaxValue)
            {
                result = default(Fixed8);
                return TR.Exit(false);
            }
            result = new Fixed8
            {
                value = (long)d
            };
            return TR.Exit(true);
        }

        public static explicit operator decimal(Fixed8 value)
        {
            TR.Enter();
            return TR.Exit(value.value / (decimal)D);
        }

        public static explicit operator long(Fixed8 value)
        {
            TR.Enter();
            return TR.Exit(value.value / D);
        }

        public static bool operator ==(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(x.Equals(y));
        }

        public static bool operator !=(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(!x.Equals(y));
        }

        public static bool operator >(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) > 0);
        }

        public static bool operator <(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) < 0);
        }

        public static bool operator >=(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) >= 0);
        }

        public static bool operator <=(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            return TR.Exit(x.CompareTo(y) <= 0);
        }

        public static Fixed8 operator *(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            const ulong QUO = (1ul << 63) / (D >> 1);
            const ulong REM = ((1ul << 63) % (D >> 1)) << 1;
            int sign = Math.Sign(x.value) * Math.Sign(y.value);
            ulong ux = (ulong)Math.Abs(x.value);
            ulong uy = (ulong)Math.Abs(y.value);
            ulong xh = ux >> 32;
            ulong xl = ux & 0x00000000fffffffful;
            ulong yh = uy >> 32;
            ulong yl = uy & 0x00000000fffffffful;
            ulong rh = xh * yh;
            ulong rm = xh * yl + xl * yh;
            ulong rl = xl * yl;
            ulong rmh = rm >> 32;
            ulong rml = rm << 32;
            rh += rmh;
            rl += rml;
            if (rl < rml)
                ++rh;
            if (rh >= D)
                throw new OverflowException();
            ulong rd = rh * REM + rl;
            if (rd < rl)
                ++rh;
            ulong r = rh * QUO + rd / D;
            x.value = (long)r * sign;
            return TR.Exit(x);
        }

        public static Fixed8 operator *(Fixed8 x, long y)
        {
            TR.Enter();
            x.value *= y;
            return TR.Exit(x);
        }

        public static Fixed8 operator /(Fixed8 x, long y)
        {
            TR.Enter();
            x.value /= y;
            return TR.Exit(x);
        }

        public static Fixed8 operator +(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            x.value = checked(x.value + y.value);
            return TR.Exit(x);
        }

        public static Fixed8 operator -(Fixed8 x, Fixed8 y)
        {
            TR.Enter();
            x.value = checked(x.value - y.value);
            return TR.Exit(x);
        }

        public static Fixed8 operator -(Fixed8 value)
        {
            TR.Enter();
            value.value = -value.value;
            return TR.Exit(value);
        }
    }
}
