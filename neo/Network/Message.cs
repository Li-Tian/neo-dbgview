﻿using NoDbgViewTR;
using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    public class Message : ISerializable
    {
        private const int PayloadMaxSize = 0x02000000;

        public static readonly uint Magic = Settings.Default.Magic;
        public string Command;
        public uint Checksum;
        public byte[] Payload;

        public int Size => sizeof(uint) + 12 + sizeof(int) + sizeof(uint) + Payload.Length;

        public static Message Create(string command, ISerializable payload = null)
        {
            TR.Enter();
            return TR.Exit(Create(command, payload == null ? new byte[0] : payload.ToArray()));
        }

        public static Message Create(string command, byte[] payload)
        {
            TR.Enter();
            return TR.Exit(new Message
            {
                Command = command,
                Checksum = GetChecksum(payload),
                Payload = payload
            });
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            TR.Enter();
            if (reader.ReadUInt32() != Magic)
            {
                TR.Exit();
                throw new FormatException();
            }
            this.Command = reader.ReadFixedString(12);
            uint length = reader.ReadUInt32();
            if (length > PayloadMaxSize)
            {
                TR.Exit();
                throw new FormatException();
            }
            this.Checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes((int)length);
            if (GetChecksum(Payload) != Checksum)
            {
                TR.Exit();
                throw new FormatException();
            }
            TR.Exit();
        }

        public static async Task<Message> DeserializeFromAsync(Stream stream, CancellationToken cancellationToken)
        {
            TR.Enter();
            uint payload_length;
            IndentContext ic = TR.SaveContextAndShuffle();
            byte[] buffer = await FillBufferAsync(stream, 24, cancellationToken);
            TR.RestoreContext(ic);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                {
                    TR.Exit();
                    throw new FormatException();
                }
                message.Command = reader.ReadFixedString(12);
                payload_length = reader.ReadUInt32();
                if (payload_length > PayloadMaxSize)
                {
                    TR.Exit();
                    throw new FormatException();
                }
                message.Checksum = reader.ReadUInt32();
            }
            if (payload_length > 0)
            {
                ic = TR.SaveContextAndShuffle();
                message.Payload = await FillBufferAsync(stream, (int)payload_length, cancellationToken);
                TR.RestoreContext(ic);
            }
            else
                message.Payload = new byte[0];
            if (GetChecksum(message.Payload) != message.Checksum)
            {
                TR.Exit();
                throw new FormatException();
            }
            return TR.Exit(message);
        }

        public static async Task<Message> DeserializeFromAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            TR.Enter();
            uint payload_length;
            byte[] buffer = await FillBufferAsync(socket, 24, cancellationToken);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                {
                    TR.Exit();
                    throw new FormatException();
                }
                message.Command = reader.ReadFixedString(12);
                payload_length = reader.ReadUInt32();
                if (payload_length > PayloadMaxSize)
                {
                    TR.Exit();
                    throw new FormatException();
                }
                message.Checksum = reader.ReadUInt32();
            }
            if (payload_length > 0)
                message.Payload = await FillBufferAsync(socket, (int)payload_length, cancellationToken);
            else
                message.Payload = new byte[0];
            if (GetChecksum(message.Payload) != message.Checksum)
            {
                TR.Exit();
                throw new FormatException();
            }
            return TR.Exit(message);
        }

        private static async Task<byte[]> FillBufferAsync(Stream stream, int buffer_size, CancellationToken cancellationToken)
        {
            TR.Enter();
            const int MAX_SIZE = 1024;
            byte[] buffer = new byte[buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE];
            using (MemoryStream ms = new MemoryStream())
            {
                while (buffer_size > 0)
                {
                    int count = buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE;
                    IndentContext ic = TR.SaveContextAndShuffle();
                    count = await stream.ReadAsync(buffer, 0, count, cancellationToken);
                    TR.RestoreContext(ic);
                    if (count <= 0)
                    {
                        TR.Exit();
                        throw new IOException();
                    }
                    ms.Write(buffer, 0, count);
                    buffer_size -= count;
                }
                return TR.Exit(ms.ToArray());
            }
        }

        private static async Task<byte[]> FillBufferAsync(WebSocket socket, int buffer_size, CancellationToken cancellationToken)
        {
            TR.Enter();
            const int MAX_SIZE = 1024;
            byte[] buffer = new byte[buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE];
            using (MemoryStream ms = new MemoryStream())
            {
                while (buffer_size > 0)
                {
                    int count = buffer_size < MAX_SIZE ? buffer_size : MAX_SIZE;
                    ArraySegment<byte> segment = new ArraySegment<byte>(buffer, 0, count);
                    WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancellationToken);
                    if (result.Count <= 0 || result.MessageType != WebSocketMessageType.Binary)
                    {
                        TR.Exit();
                        throw new IOException();
                    }
                    ms.Write(buffer, 0, result.Count);
                    buffer_size -= result.Count;
                }
                return TR.Exit(ms.ToArray());
            }
        }

        private static uint GetChecksum(byte[] value)
        {
            TR.Enter();
            return TR.Exit(Crypto.Default.Hash256(value).ToUInt32(0));
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            TR.Enter();
            writer.Write(Magic);
            writer.WriteFixedString(Command, 12);
            writer.Write(Payload.Length);
            writer.Write(Checksum);
            writer.Write(Payload);
            TR.Exit();
        }
    }
}
