using System;
using System.IO;
using System.Reflection;
using System.Text;
using NoDbgViewTR;

namespace Neo.IO.Json
{
    public class JNumber : JObject
    {
        public double Value { get; private set; }

        public JNumber(double value = 0)
        {
            TR.Enter();
            this.Value = value;
            TR.Exit();
        }

        public override bool AsBoolean()
        {
            TR.Enter();
            if (Value == 0)
                return TR.Exit(false);
            return TR.Exit(true);
        }

        public override T AsEnum<T>(bool ignoreCase = false)
        {
            TR.Enter();
            Type t = typeof(T);
            TypeInfo ti = t.GetTypeInfo();
            if (!ti.IsEnum)
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            if (ti.GetEnumUnderlyingType() == typeof(byte))
                return TR.Exit((T)Enum.ToObject(t, (byte)Value));
            if (ti.GetEnumUnderlyingType() == typeof(int))
                return TR.Exit((T)Enum.ToObject(t, (int)Value));
            if (ti.GetEnumUnderlyingType() == typeof(long))
                return TR.Exit((T)Enum.ToObject(t, (long)Value));
            if (ti.GetEnumUnderlyingType() == typeof(sbyte))
                return TR.Exit((T)Enum.ToObject(t, (sbyte)Value));
            if (ti.GetEnumUnderlyingType() == typeof(short))
                return TR.Exit((T)Enum.ToObject(t, (short)Value));
            if (ti.GetEnumUnderlyingType() == typeof(uint))
                return TR.Exit((T)Enum.ToObject(t, (uint)Value));
            if (ti.GetEnumUnderlyingType() == typeof(ulong))
                return TR.Exit((T)Enum.ToObject(t, (ulong)Value));
            if (ti.GetEnumUnderlyingType() == typeof(ushort))
                return TR.Exit((T)Enum.ToObject(t, (ushort)Value));
            TR.Exit();
            throw new InvalidCastException();
        }

        public override double AsNumber()
        {
            TR.Enter();
            return TR.Exit(Value);
        }

        public override string AsString()
        {
            TR.Enter();
            return TR.Exit(Value.ToString());
        }

        public override bool CanConvertTo(Type type)
        {
            TR.Enter();
            if (type == typeof(bool))
                return TR.Exit(true);
            if (type == typeof(double))
                return TR.Exit(true);
            if (type == typeof(string))
                return TR.Exit(true);
            TypeInfo ti = type.GetTypeInfo();
            if (ti.IsEnum && Enum.IsDefined(type, Convert.ChangeType(Value, ti.GetEnumUnderlyingType())))
                return TR.Exit(true);
            return TR.Exit(false);
        }

        internal new static JNumber Parse(TextReader reader)
        {
            TR.Enter();
            SkipSpace(reader);
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)reader.Peek();
                if (c >= '0' && c <= '9' || c == '.' || c == '-')
                {
                    sb.Append(c);
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
            return TR.Exit(new JNumber(double.Parse(sb.ToString())));
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(Value.ToString());
        }

        public DateTime ToTimestamp()
        {
            TR.Enter();
            if (Value < 0 || Value > ulong.MaxValue)
            {
                TR.Exit();
                throw new InvalidCastException();
            }
            return TR.Exit(((ulong)Value).ToDateTime());
        }
    }
}
