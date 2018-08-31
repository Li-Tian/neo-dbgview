using System.Collections;
using System.Linq;
using NoDbgViewTR;

namespace Neo.Cryptography
{
    public class BloomFilter
    {
        private readonly uint[] seeds;
        private readonly BitArray bits;

        public int K => seeds.Length;

        public int M => bits.Length;

        public uint Tweak { get; private set; }

        public BloomFilter(int m, int k, uint nTweak, byte[] elements = null)
        {
            TR.Enter();
            this.seeds = Enumerable.Range(0, k).Select(p => (uint)p * 0xFBA4C795 + nTweak).ToArray();
            this.bits = elements == null ? new BitArray(m) : new BitArray(elements);
            this.bits.Length = m;
            this.Tweak = nTweak;
            TR.Exit();
        }

        public void Add(byte[] element)
        {
            TR.Enter();
            foreach (uint i in seeds.AsParallel().Select(s => element.Murmur32(s)))
                bits.Set((int)(i % (uint)bits.Length), true);
            TR.Exit();
        }

        public bool Check(byte[] element)
        {
            TR.Enter();
            foreach (uint i in seeds.AsParallel().Select(s => element.Murmur32(s)))
                if (!bits.Get((int)(i % (uint)bits.Length)))
                    return TR.Exit(false);
            return TR.Exit(true);
        }

        public void GetBits(byte[] newBits)
        {
            TR.Enter();
            bits.CopyTo(newBits, 0);
            TR.Exit();
        }
    }
}
