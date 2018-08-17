using Neo.IO;
using System.IO;
using DbgViewTR;

namespace Neo.Network.Payloads
{
    public class AddrPayload : ISerializable
    {
        public NetworkAddressWithTime[] AddressList;

        public int Size => AddressList.GetVarSize();

        public static AddrPayload Create(params NetworkAddressWithTime[] addresses)
        {
            TR.Enter();
            return TR.Exit(new AddrPayload
            {
                AddressList = addresses
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            this.AddressList = reader.ReadSerializableArray<NetworkAddressWithTime>(200);
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(AddressList);
            TR.Exit();
        }
    }
}
