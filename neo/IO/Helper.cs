using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NoDbgViewTR;

namespace Neo.IO
{
    public static class Helper
    {
        public static T AsSerializable<T>(this byte[] value, int start = 0) where T : ISerializable, new()
        {
            TR.Enter();
            using (MemoryStream ms = new MemoryStream(value, start, value.Length - start, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return TR.Exit(reader.ReadSerializable<T>());
            }
        }

        public static ISerializable AsSerializable(this byte[] value, Type type)
        {
            TR.Enter();
            if (!typeof(ISerializable).GetTypeInfo().IsAssignableFrom(type))
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            ISerializable serializable = (ISerializable)Activator.CreateInstance(type);
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                serializable.Deserialize(reader);
            }
            return TR.Exit(serializable);
        }

        public static T[] AsSerializableArray<T>(this byte[] value, int max = 0x10000000) where T : ISerializable, new()
        {
            TR.Enter();
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return TR.Exit(reader.ReadSerializableArray<T>(max));
            }
        }

        internal static int GetVarSize(int value)
        {
            TR.Enter();
            if (value < 0xFD)
                return TR.Exit(sizeof(byte));
            else if (value <= 0xFFFF)
                return TR.Exit(sizeof(byte) + sizeof(ushort));
            else
                return TR.Exit(sizeof(byte) + sizeof(uint));
        }

        internal static int GetVarSize<T>(this T[] value)
        {
            TR.Enter();
            int value_size;
            Type t = typeof(T);
            if (typeof(ISerializable).IsAssignableFrom(t))
            {
                value_size = value.OfType<ISerializable>().Sum(p => p.Size);
            }
            else if (t.GetTypeInfo().IsEnum)
            {
                int element_size;
                Type u = t.GetTypeInfo().GetEnumUnderlyingType();
                if (u == typeof(sbyte) || u == typeof(byte))
                    element_size = 1;
                else if (u == typeof(short) || u == typeof(ushort))
                    element_size = 2;
                else if (u == typeof(int) || u == typeof(uint))
                    element_size = 4;
                else //if (u == typeof(long) || u == typeof(ulong))
                    element_size = 8;
                value_size = value.Length * element_size;
            }
            else
            {
                value_size = value.Length * Marshal.SizeOf<T>();
            }
            return TR.Exit(GetVarSize(value.Length) + value_size);
        }

        internal static int GetVarSize(this string value)
        {
            TR.Enter();
            int size = Encoding.UTF8.GetByteCount(value);
            return TR.Exit(GetVarSize(size) + size);
        }

        public static byte[] ReadBytesWithGrouping(this BinaryReader reader)
        {
            TR.Enter();
            const int GROUP_SIZE = 16;
            using (MemoryStream ms = new MemoryStream())
            {
                int padding = 0;
                do
                {
                    byte[] group = reader.ReadBytes(GROUP_SIZE);
                    padding = reader.ReadByte();
                    if (padding > GROUP_SIZE)
                    {
                        TR.Exit();
                        throw new FormatException();
                    }
                    int count = GROUP_SIZE - padding;
                    if (count > 0)
                        ms.Write(group, 0, count);
                } while (padding == 0);
                return TR.Exit(ms.ToArray());
            }
        }

        public static string ReadFixedString(this BinaryReader reader, int length)
        {
            TR.Enter();
            byte[] data = reader.ReadBytes(length);
            return TR.Exit(Encoding.UTF8.GetString(data.TakeWhile(p => p != 0).ToArray()));
        }

        public static T ReadSerializable<T>(this BinaryReader reader) where T : ISerializable, new()
        {
            TR.Enter();
            T obj = new T();
            obj.Deserialize(reader);
            return TR.Exit(obj);
        }

        public static T[] ReadSerializableArray<T>(this BinaryReader reader, int max = 0x10000000) where T : ISerializable, new()
        {
            TR.Enter();
            T[] array = new T[reader.ReadVarInt((ulong)max)];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new T();
                array[i].Deserialize(reader);
            }
            return TR.Exit(array);
        }

        public static byte[] ReadVarBytes(this BinaryReader reader, int max = 0X7fffffc7)
        {
            TR.Enter();
            return TR.Exit(reader.ReadBytes((int)reader.ReadVarInt((ulong)max)));
        }

        public static ulong ReadVarInt(this BinaryReader reader, ulong max = ulong.MaxValue)
        {
            TR.Enter();
            byte fb = reader.ReadByte();
            ulong value;
            if (fb == 0xFD)
                value = reader.ReadUInt16();
            else if (fb == 0xFE)
                value = reader.ReadUInt32();
            else if (fb == 0xFF)
                value = reader.ReadUInt64();
            else
                value = fb;
            if (value > max)
            {
                TR.Exit();
                throw new FormatException();
            }
            return TR.Exit(value);
        }

        public static string ReadVarString(this BinaryReader reader, int max = 0X7fffffc7)
        {
            TR.Enter();
            return TR.Exit(Encoding.UTF8.GetString(reader.ReadVarBytes(max)));
        }

        public static byte[] ToArray(this ISerializable value)
        {
            TR.Enter();
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                value.Serialize(writer);
                writer.Flush();
                return TR.Exit(ms.ToArray());
            }
        }

        public static byte[] ToByteArray<T>(this T[] value) where T : ISerializable
        {
            TR.Enter();
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write(value);
                writer.Flush();
                return TR.Exit(ms.ToArray());
            }
        }

        public static void Write(this BinaryWriter writer, ISerializable value)
        {
            TR.Enter();
            value.Serialize(writer);
            TR.Exit();
        }

        public static void Write<T>(this BinaryWriter writer, T[] value) where T : ISerializable
        {
            TR.Enter();
            writer.WriteVarInt(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                value[i].Serialize(writer);
            }
            TR.Exit();
        }

        public static void WriteBytesWithGrouping(this BinaryWriter writer, byte[] value)
        {
            TR.Enter();
            const int GROUP_SIZE = 16;
            int index = 0;
            int remain = value.Length;
            while (remain >= GROUP_SIZE)
            {
                writer.Write(value, index, GROUP_SIZE);
                writer.Write((byte)0);
                index += GROUP_SIZE;
                remain -= GROUP_SIZE;
            }
            if (remain > 0)
                writer.Write(value, index, remain);
            int padding = GROUP_SIZE - remain;
            for (int i = 0; i < padding; i++)
                writer.Write((byte)0);
            writer.Write((byte)padding);
            TR.Exit();
        }

        public static void WriteFixedString(this BinaryWriter writer, string value, int length)
        {
            TR.Enter();
            if (value == null)
            {
                TR.Exit();
                throw new ArgumentNullException(nameof(value));
            }
            if (value.Length > length)
            {
                TR.Exit();
                throw new ArgumentException();
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > length)
            {
                TR.Exit();
                throw new ArgumentException();
            }
            writer.Write(bytes);
            if (bytes.Length < length)
                writer.Write(new byte[length - bytes.Length]);
            TR.Exit();
        }

        public static void WriteVarBytes(this BinaryWriter writer, byte[] value)
        {
            TR.Enter();
            writer.WriteVarInt(value.Length);
            writer.Write(value);
            TR.Exit();
        }

        public static void WriteVarInt(this BinaryWriter writer, long value)
        {
            TR.Enter();
            if (value < 0)
            {
                TR.Exit();
                throw new ArgumentOutOfRangeException();
            }
            if (value < 0xFD)
            {
                writer.Write((byte)value);
            }
            else if (value <= 0xFFFF)
            {
                writer.Write((byte)0xFD);
                writer.Write((ushort)value);
            }
            else if (value <= 0xFFFFFFFF)
            {
                writer.Write((byte)0xFE);
                writer.Write((uint)value);
            }
            else
            {
                writer.Write((byte)0xFF);
                writer.Write(value);
            }
            TR.Exit();
        }

        public static void WriteVarString(this BinaryWriter writer, string value)
        {
            TR.Enter();
            writer.WriteVarBytes(Encoding.UTF8.GetBytes(value));
            TR.Exit();
        }
    }
}
