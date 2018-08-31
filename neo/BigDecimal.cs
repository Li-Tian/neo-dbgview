﻿using System;
using System.Numerics;
using NoDbgViewTR;

namespace Neo
{
    public struct BigDecimal
    {
        private readonly BigInteger value;
        private readonly byte decimals;

        public BigInteger Value => value;
        public byte Decimals => decimals;
        public int Sign => value.Sign;

        public BigDecimal(BigInteger value, byte decimals)
        {
            TR.Enter();
            this.value = value;
            this.decimals = decimals;
            TR.Exit();
        }

        public BigDecimal ChangeDecimals(byte decimals)
        {
            TR.Enter();
            if (this.decimals == decimals) return TR.Exit(this);
            BigInteger value;
            if (this.decimals < decimals)
            {
                value = this.value * BigInteger.Pow(10, decimals - this.decimals);
            }
            else
            {
                BigInteger divisor = BigInteger.Pow(10, this.decimals - decimals);
                value = BigInteger.DivRem(this.value, divisor, out BigInteger remainder);
                if (remainder > BigInteger.Zero)
                    throw new ArgumentOutOfRangeException();
            }
            return TR.Exit(new BigDecimal(value, decimals));
        }

        public static BigDecimal Parse(string s, byte decimals)
        {
            TR.Enter();
            if (!TryParse(s, decimals, out BigDecimal result))
                throw new FormatException();
            return TR.Exit(result);
        }

        public Fixed8 ToFixed8()
        {
            TR.Enter();
            try
            {
                return TR.Exit(new Fixed8((long)ChangeDecimals(8).value));
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(ex.Message, ex);
            }
        }

        public override string ToString()
        {
            TR.Enter();
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger result = BigInteger.DivRem(value, divisor, out BigInteger remainder);
            if (remainder == 0) return TR.Exit(result.ToString());
            return TR.Exit($"{result}.{remainder.ToString("d" + decimals)}".TrimEnd('0'));
        }

        public static bool TryParse(string s, byte decimals, out BigDecimal result)
        {
            TR.Enter();
            int e = 0;
            int index = s.IndexOfAny(new[] { 'e', 'E' });
            if (index >= 0)
            {
                if (!sbyte.TryParse(s.Substring(index + 1), out sbyte e_temp))
                {
                    result = default(BigDecimal);
                    return TR.Exit(false);
                }
                e = e_temp;
                s = s.Substring(0, index);
            }
            index = s.IndexOf('.');
            if (index >= 0)
            {
                s = s.TrimEnd('0');
                e -= s.Length - index - 1;
                s = s.Remove(index, 1);
            }
            int ds = e + decimals;
            if (ds < 0)
            {
                result = default(BigDecimal);
                return TR.Exit(false);
            }
            if (ds > 0)
                s += new string('0', ds);
            if (!BigInteger.TryParse(s, out BigInteger value))
            {
                result = default(BigDecimal);
                return TR.Exit(false);
            }
            result = new BigDecimal(value, decimals);
            return TR.Exit(true);
        }
    }
}
