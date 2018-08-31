using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using NoDbgViewTR;

namespace Neo.IO.Json
{
    public class JString : JObject
    {
        public string Value { get; private set; }

        public JString(string value)
        {
            TR.Enter();
            if (value == null)
            {
                TR.Exit();
                throw new ArgumentNullException();
            }
            this.Value = value;
            TR.Exit();
        }

        public override bool AsBoolean()
        {
            TR.Enter();
            switch (Value.ToLower())
            {
                case "0":
                case "f":
                case "false":
                case "n":
                case "no":
                case "off":
                    return TR.Exit(false);
                default:
                    return TR.Exit(true);
            }
        }

        public override T AsEnum<T>(bool ignoreCase = false)
        {
            TR.Enter();
            try
            {
                return TR.Exit((T)Enum.Parse(typeof(T), Value, ignoreCase));
            }
            catch
            {
                TR.Exit();
                throw new InvalidCastException();
            }
        }

        public override double AsNumber()
        {
            TR.Enter();
            try
            {
                return TR.Exit(double.Parse(Value));
            }
            catch
            {
                TR.Exit();
                throw new InvalidCastException();
            }
        }

        public override string AsString()
        {
            TR.Enter();
            return TR.Exit(Value);
        }

        public override bool CanConvertTo(Type type)
        {
            TR.Enter();
            if (type == typeof(bool))
                return TR.Exit(true);
            if (type.GetTypeInfo().IsEnum && Enum.IsDefined(type, Value))
                return TR.Exit(true);
            if (type == typeof(double))
                return TR.Exit(true);
            if (type == typeof(string))
                return TR.Exit(true);
            return TR.Exit(false);
        }

        internal new static JString Parse(TextReader reader)
        {
            TR.Enter();
            SkipSpace(reader);
            char[] buffer = new char[4];
            char firstChar = (char)reader.Read();
            if (firstChar != '\"' && firstChar != '\'')
            {
                TR.Exit();
                throw new FormatException();
            }
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)reader.Read();
                if (c == 65535)
                {
                    TR.Exit();
                    throw new FormatException();
                }
                if (c == firstChar) break;
                if (c == '\\')
                {
                    c = (char)reader.Read();
                    if (c == 'u')
                    {
                        reader.Read(buffer, 0, 4);
                        c = (char)short.Parse(new string(buffer), NumberStyles.HexNumber);
                    }
                }
                sb.Append(c);
            }
            return TR.Exit(new JString(sb.ToString()));
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit($"\"{JavaScriptEncoder.Default.Encode(Value)}\"");
        }
    }
}
