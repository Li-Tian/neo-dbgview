using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using DbgViewTR;

namespace Neo.Cryptography.ECC
{
    internal class ECFieldElement : IComparable<ECFieldElement>, IEquatable<ECFieldElement>
    {
        internal readonly BigInteger Value;
        private readonly ECCurve curve;

        public ECFieldElement(BigInteger value, ECCurve curve)
        {
            TR.Enter();
            if (value >= curve.Q)
                throw new ArgumentException("x value too large in field element");
            this.Value = value;
            this.curve = curve;
            TR.Exit();
        }

        public int CompareTo(ECFieldElement other)
        {
            TR.Enter();
            if (ReferenceEquals(this, other)) return 0;
            return TR.Exit(Value.CompareTo(other.Value));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            if (obj == this)
                return TR.Exit(true);

            ECFieldElement other = obj as ECFieldElement;

            if (other == null)
                return TR.Exit(false);

            return TR.Exit(Equals(other));
        }

        public bool Equals(ECFieldElement other)
        {
            return TR.Log(Value.Equals(other.Value));
        }

        private static BigInteger[] FastLucasSequence(BigInteger p, BigInteger P, BigInteger Q, BigInteger k)
        {
            TR.Enter();
            int n = k.GetBitLength();
            int s = k.GetLowestSetBit();

            Debug.Assert(k.TestBit(s));

            BigInteger Uh = 1;
            BigInteger Vl = 2;
            BigInteger Vh = P;
            BigInteger Ql = 1;
            BigInteger Qh = 1;

            for (int j = n - 1; j >= s + 1; --j)
            {
                Ql = (Ql * Qh).Mod(p);

                if (k.TestBit(j))
                {
                    Qh = (Ql * Q).Mod(p);
                    Uh = (Uh * Vh).Mod(p);
                    Vl = (Vh * Vl - P * Ql).Mod(p);
                    Vh = ((Vh * Vh) - (Qh << 1)).Mod(p);
                }
                else
                {
                    Qh = Ql;
                    Uh = (Uh * Vl - Ql).Mod(p);
                    Vh = (Vh * Vl - P * Ql).Mod(p);
                    Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                }
            }

            Ql = (Ql * Qh).Mod(p);
            Qh = (Ql * Q).Mod(p);
            Uh = (Uh * Vl - Ql).Mod(p);
            Vl = (Vh * Vl - P * Ql).Mod(p);
            Ql = (Ql * Qh).Mod(p);

            for (int j = 1; j <= s; ++j)
            {
                Uh = Uh * Vl * p;
                Vl = ((Vl * Vl) - (Ql << 1)).Mod(p);
                Ql = (Ql * Ql).Mod(p);
            }

            return TR.Exit(new BigInteger[] { Uh, Vl });
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(Value.GetHashCode());
        }

        public ECFieldElement Sqrt()
        {
            TR.Enter();
            if (curve.Q.TestBit(1))
            {
                ECFieldElement z = new ECFieldElement(BigInteger.ModPow(Value, (curve.Q >> 2) + 1, curve.Q), curve);
                return TR.Exit(z.Square().Equals(this) ? z : null);
            }
            BigInteger qMinusOne = curve.Q - 1;
            BigInteger legendreExponent = qMinusOne >> 1;
            if (BigInteger.ModPow(Value, legendreExponent, curve.Q) != 1)
            {
                TR.Exit();
                return null;
            }
           
            BigInteger u = qMinusOne >> 2;
            BigInteger k = (u << 1) + 1;
            BigInteger Q = this.Value;
            BigInteger fourQ = (Q << 2).Mod(curve.Q);
            BigInteger U, V;
            do
            {
                Random rand = new Random();
                BigInteger P;
                do
                {
                    P = rand.NextBigInteger(curve.Q.GetBitLength());
                }
                while (P >= curve.Q || BigInteger.ModPow(P * P - fourQ, legendreExponent, curve.Q) != qMinusOne);
                BigInteger[] result = FastLucasSequence(curve.Q, P, Q, k);
                U = result[0];
                V = result[1];
                if ((V * V).Mod(curve.Q) == fourQ)
                {
                    if (V.TestBit(0))
                    {
                        V += curve.Q;
                    }
                    V >>= 1;
                    Debug.Assert((V * V).Mod(curve.Q) == Value);
                    return new ECFieldElement(V, curve);
                }
            }
            while (U.Equals(BigInteger.One) || U.Equals(qMinusOne));
            TR.Exit();
            return null;
        }

        public ECFieldElement Square()
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((Value * Value).Mod(curve.Q), curve));
        }

        public byte[] ToByteArray()
        {
            TR.Enter();
            byte[] data = Value.ToByteArray();
            if (data.Length == 32)
                return TR.Exit(data.Reverse().ToArray());
            if (data.Length > 32)
                return TR.Exit(data.Take(32).Reverse().ToArray());
            return TR.Exit(Enumerable.Repeat<byte>(0, 32 - data.Length).Concat(data.Reverse()).ToArray());
        }

        public static ECFieldElement operator -(ECFieldElement x)
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((-x.Value).Mod(x.curve.Q), x.curve));
        }

        public static ECFieldElement operator *(ECFieldElement x, ECFieldElement y)
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((x.Value * y.Value).Mod(x.curve.Q), x.curve));
        }

        public static ECFieldElement operator /(ECFieldElement x, ECFieldElement y)
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((x.Value * y.Value.ModInverse(x.curve.Q)).Mod(x.curve.Q), x.curve));
        }

        public static ECFieldElement operator +(ECFieldElement x, ECFieldElement y)
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((x.Value + y.Value).Mod(x.curve.Q), x.curve));
        }

        public static ECFieldElement operator -(ECFieldElement x, ECFieldElement y)
        {
            TR.Enter();
            return TR.Exit(new ECFieldElement((x.Value - y.Value).Mod(x.curve.Q), x.curve));
        }
    }
}
