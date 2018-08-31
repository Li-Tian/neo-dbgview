using Neo.IO;
using System;
using System.IO;
using System.Linq;
using System.Net;
using NoDbgViewTR;

namespace Neo.Network.Payloads
{
    public class NetworkAddressWithTime : ISerializable
    {
        public const ulong NODE_NETWORK = 1;

        public uint Timestamp;
        public ulong Services;
        public IPEndPoint EndPoint;

        public int Size => sizeof(uint) + sizeof(ulong) + 16 + sizeof(ushort);

        public static NetworkAddressWithTime Create(IPEndPoint endpoint, ulong services, uint timestamp)
        {
            TR.Enter();
            return TR.Exit(new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                Services = services,
                EndPoint = endpoint
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            Timestamp = reader.ReadUInt32();
            Services = reader.ReadUInt64();
            byte[] data = reader.ReadBytes(16);
            if (data.Length != 16)
            {
                TR.Exit();
                throw new FormatException();
            }
            IPAddress address = new IPAddress(data);
            data = reader.ReadBytes(2);
            if (data.Length != 2)
            {
                TR.Exit();
                throw new FormatException();
            }
            ushort port = data.Reverse().ToArray().ToUInt16(0);
            EndPoint = new IPEndPoint(address, port);
            TR.Exit();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(Timestamp);
            writer.Write(Services);
            writer.Write(EndPoint.Address.MapToIPv6().GetAddressBytes());
            writer.Write(BitConverter.GetBytes((ushort)EndPoint.Port).Reverse().ToArray());
            TR.Exit();
        }
    }
}
