using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using DbgViewTR;

namespace Neo
{
    public static class Helper
    {
        private static readonly DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static int BitLen(int w)
        {
            TR.Enter();
            return TR.Exit((w < 1 << 15 ? (w < 1 << 7
                ? (w < 1 << 3 ? (w < 1 << 1
                ? (w < 1 << 0 ? (w < 0 ? 32 : 0) : 1)
                : (w < 1 << 2 ? 2 : 3)) : (w < 1 << 5
                ? (w < 1 << 4 ? 4 : 5)
                : (w < 1 << 6 ? 6 : 7)))
                : (w < 1 << 11
                ? (w < 1 << 9 ? (w < 1 << 8 ? 8 : 9) : (w < 1 << 10 ? 10 : 11))
                : (w < 1 << 13 ? (w < 1 << 12 ? 12 : 13) : (w < 1 << 14 ? 14 : 15)))) : (w < 1 << 23 ? (w < 1 << 19
                ? (w < 1 << 17 ? (w < 1 << 16 ? 16 : 17) : (w < 1 << 18 ? 18 : 19))
                : (w < 1 << 21 ? (w < 1 << 20 ? 20 : 21) : (w < 1 << 22 ? 22 : 23))) : (w < 1 << 27
                ? (w < 1 << 25 ? (w < 1 << 24 ? 24 : 25) : (w < 1 << 26 ? 26 : 27))
                : (w < 1 << 29 ? (w < 1 << 28 ? 28 : 29) : (w < 1 << 30 ? 30 : 31))))));
        }

        internal static int GetBitLength(this BigInteger i)
        {
            TR.Enter();
            byte[] b = i.ToByteArray();
            return TR.Exit((b.Length - 1) * 8 + BitLen(i.Sign > 0 ? b[b.Length - 1] : 255 - b[b.Length - 1]));
        }

        internal static int GetLowestSetBit(this BigInteger i)
        {
            TR.Enter();
            if (i.Sign == 0)
                return TR.Exit(-1);
            byte[] b = i.ToByteArray();
            int w = 0;
            while (b[w] == 0)
                w++;
            for (int x = 0; x < 8; x++)
                if ((b[w] & 1 << x) > 0)
                    return TR.Exit(x + w * 8);
            throw new Exception();
        }

        internal static string GetVersion(this Assembly assembly)
        {
            TR.Enter();
            CustomAttributeData attribute = assembly.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(AssemblyInformationalVersionAttribute));
            if (attribute == null) return TR.Exit(assembly.GetName().Version.ToString(3));
            return TR.Exit((string)attribute.ConstructorArguments[0].Value);
        }

        public static byte[] HexToBytes(this string value)
        {
            TR.Enter();
            if (value == null || value.Length == 0)
                return TR.Exit(new byte[0]);
            if (value.Length % 2 == 1)
                throw new FormatException();
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = byte.Parse(value.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier);
            return TR.Exit(result);
        }

        internal static BigInteger Mod(this BigInteger x, BigInteger y)
        {
            TR.Enter();
            x %= y;
            if (x.Sign < 0)
                x += y;
            return TR.Exit(x);
        }

        internal static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            TR.Enter();
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return TR.Exit(v);
        }

        internal static BigInteger NextBigInteger(this Random rand, int sizeInBits)
        {
            TR.Enter();
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return TR.Exit(0);
            byte[] b = new byte[sizeInBits / 8 + 1];
            rand.NextBytes(b);
            if (sizeInBits % 8 == 0)
                b[b.Length - 1] = 0;
            else
                b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
            return TR.Exit(new BigInteger(b));
        }

        internal static BigInteger NextBigInteger(this RandomNumberGenerator rng, int sizeInBits)
        {
            TR.Enter();
            if (sizeInBits < 0)
                throw new ArgumentException("sizeInBits must be non-negative");
            if (sizeInBits == 0)
                return TR.Exit(0);
            byte[] b = new byte[sizeInBits / 8 + 1];
            rng.GetBytes(b);
            if (sizeInBits % 8 == 0)
                b[b.Length - 1] = 0;
            else
                b[b.Length - 1] &= (byte)((1 << sizeInBits % 8) - 1);
            return TR.Exit(new BigInteger(b));
        }

        public static Fixed8 Sum(this IEnumerable<Fixed8> source)
        {
            TR.Enter();
            long sum = 0;
            checked
            {
                foreach (Fixed8 item in source)
                {
                    sum += item.value;
                }
            }
            return TR.Exit(new Fixed8(sum));
        }

        public static Fixed8 Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, Fixed8> selector)
        {
            TR.Enter();
            return TR.Exit(source.Select(selector).Sum());
        }

        internal static bool TestBit(this BigInteger i, int index)
        {
            TR.Enter();
            return TR.Exit((i & (BigInteger.One << index)) > BigInteger.Zero);
        }

        public static DateTime ToDateTime(this uint timestamp)
        {
            TR.Enter();
            return TR.Exit(unixEpoch.AddSeconds(timestamp).ToLocalTime());
        }

        public static DateTime ToDateTime(this ulong timestamp)
        {
            TR.Enter();
            return TR.Exit(unixEpoch.AddSeconds(timestamp).ToLocalTime());
        }

        public static string ToHexString(this IEnumerable<byte> value)
        {
            TR.Enter();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in value)
                sb.AppendFormat("{0:x2}", b);
            return TR.Exit(sb.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static int ToInt32(this byte[] value, int startIndex)
        {
            TR.Enter();
            fixed (byte* pbyte = &value[startIndex])
            {
                return TR.Exit(*((int*)pbyte));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static long ToInt64(this byte[] value, int startIndex)
        {
            TR.Enter();
            fixed (byte* pbyte = &value[startIndex])
            {
                return TR.Exit(*((long*)pbyte));
            }
        }

        public static uint ToTimestamp(this DateTime time)
        {
            TR.Enter();
            return TR.Exit((uint)(time.ToUniversalTime() - unixEpoch).TotalSeconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static ushort ToUInt16(this byte[] value, int startIndex)
        {
            TR.Enter();
            fixed (byte* pbyte = &value[startIndex])
            {
                return TR.Exit(*((ushort*)pbyte));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static uint ToUInt32(this byte[] value, int startIndex)
        {
            TR.Enter();
            fixed (byte* pbyte = &value[startIndex])
            {
                return TR.Exit(*((uint*)pbyte));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static ulong ToUInt64(this byte[] value, int startIndex)
        {
            TR.Enter();
            fixed (byte* pbyte = &value[startIndex])
            {
                return TR.Exit(*((ulong*)pbyte));
            }
        }

        internal static long WeightedAverage<T>(this IEnumerable<T> source, Func<T, long> valueSelector, Func<T, long> weightSelector)
        {
            TR.Enter();
            long sum_weight = 0;
            long sum_value = 0;
            foreach (T item in source)
            {
                long weight = weightSelector(item);
                sum_weight += weight;
                sum_value += valueSelector(item) * weight;
            }
            if (sum_value == 0) return TR.Exit(0);
            return TR.Exit(sum_value / sum_weight);
        }

        internal static IEnumerable<TResult> WeightedFilter<T, TResult>(this IList<T> source, double start, double end, Func<T, long> weightSelector, Func<T, long, TResult> resultSelector)
        {
            TR.Enter();
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (start < 0 || start > 1) throw new ArgumentOutOfRangeException(nameof(start));
            if (end < start || start + end > 1) throw new ArgumentOutOfRangeException(nameof(end));
            if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            if (source.Count == 0 || start == end) yield break;
            double amount = source.Sum(weightSelector);
            long sum = 0;
            double current = 0;
            foreach (T item in source)
            {
                if (current >= end) break;
                long weight = weightSelector(item);
                sum += weight;
                double old = current;
                current = sum / amount;
                if (current <= start) continue;
                if (old < start)
                {
                    if (current > end)
                    {
                        weight = (long)((end - start) * amount);
                    }
                    else
                    {
                        weight = (long)((current - start) * amount);
                    }
                }
                else if (current > end)
                {
                    weight = (long)((end - old) * amount);
                }
                yield return TR.Exit(resultSelector(item, weight));
            }
        }
    }
}
