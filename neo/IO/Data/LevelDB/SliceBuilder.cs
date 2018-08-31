using System;
using System.Collections.Generic;
using System.Text;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class SliceBuilder
    {
        private List<byte> data = new List<byte>();

        private SliceBuilder()
        {
        }

        public SliceBuilder Add(byte value)
        {
            TR.Enter();
            data.Add(value);
            return TR.Exit(this);
        }

        public SliceBuilder Add(ushort value)
        {
            TR.Enter();
            data.AddRange(BitConverter.GetBytes(value));
            return TR.Exit(this);
        }

        public SliceBuilder Add(uint value)
        {
            TR.Enter();
            data.AddRange(BitConverter.GetBytes(value));
            return TR.Exit(this);
        }

        public SliceBuilder Add(long value)
        {
            TR.Enter();
            data.AddRange(BitConverter.GetBytes(value));
            return TR.Exit(this);
        }

        public SliceBuilder Add(IEnumerable<byte> value)
        {
            TR.Enter();
            data.AddRange(value);
            return TR.Exit(this);
        }

        public SliceBuilder Add(string value)
        {
            TR.Enter();
            data.AddRange(Encoding.UTF8.GetBytes(value));
            return TR.Exit(this);
        }

        public SliceBuilder Add(ISerializable value)
        {
            TR.Enter();
            data.AddRange(value.ToArray());
            return TR.Exit(this);
        }

        public static SliceBuilder Begin()
        {
            TR.Enter();
            return TR.Exit(new SliceBuilder());
        }

        public static SliceBuilder Begin(byte prefix)
        {
            TR.Enter();
            return TR.Exit(new SliceBuilder().Add(prefix));
        }

        public static implicit operator Slice(SliceBuilder value)
        {
            TR.Enter();
            return TR.Exit(value.data.ToArray());
        }
    }
}
