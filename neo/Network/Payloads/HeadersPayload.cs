using Neo.Core;
using Neo.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Network.Payloads
{
    public class HeadersPayload : ISerializable
    {
        public Header[] Headers;

        public int Size => Headers.GetVarSize();

        public static HeadersPayload Create(IEnumerable<Header> headers)
        {
            TR.Enter();
            return TR.Exit(new HeadersPayload
            {
                Headers = headers.ToArray()
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Headers = reader.ReadSerializableArray<Header>(2000);
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(Headers);
            TR.Exit();
        }
    }
}
