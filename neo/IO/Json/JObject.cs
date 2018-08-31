using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NoDbgViewTR;

namespace Neo.IO.Json
{
    public class JObject
    {
        public static readonly JObject Null = null;
        private Dictionary<string, JObject> properties = new Dictionary<string, JObject>();

        public JObject this[string name]
        {
            get
            {
                properties.TryGetValue(name, out JObject value);
                return value;
            }
            set
            {
                properties[name] = value;
            }
        }

        public IReadOnlyDictionary<string, JObject> Properties => properties;

        public virtual bool AsBoolean()
        {
            TR.Enter();
            TR.Exit();
            throw new InvalidCastException();
        }

        public bool AsBooleanOrDefault(bool value = false)
        {
            TR.Enter();
            if (!CanConvertTo(typeof(bool)))
                return TR.Exit(value);
            return TR.Exit(AsBoolean());
        }

        public virtual T AsEnum<T>(bool ignoreCase = false)
        {
            TR.Enter();
            TR.Exit();
            throw new InvalidCastException();
        }

        public T AsEnumOrDefault<T>(T value = default(T), bool ignoreCase = false)
        {
            TR.Enter();
            if (!CanConvertTo(typeof(T)))
                return TR.Exit(value);
            return TR.Exit(AsEnum<T>(ignoreCase));
        }

        public virtual double AsNumber()
        {
            TR.Enter();
            TR.Exit();
            throw new InvalidCastException();
        }

        public double AsNumberOrDefault(double value = 0)
        {
            TR.Enter();
            if (!CanConvertTo(typeof(double)))
                return TR.Exit(value);
            return TR.Exit(AsNumber());
        }

        public virtual string AsString()
        {
            TR.Enter();
            throw TR.Exit(new InvalidCastException());
        }

        public string AsStringOrDefault(string value = null)
        {
            TR.Enter();
            if (!CanConvertTo(typeof(string)))
                return TR.Exit(value);
            return TR.Exit(AsString());
        }

        public virtual bool CanConvertTo(Type type)
        {
            TR.Enter();
            return TR.Exit(false);
        }

        public bool ContainsProperty(string key)
        {
            TR.Enter();
            return TR.Exit(properties.ContainsKey(key));
        }

        public static JObject Parse(TextReader reader)
        {
            TR.Enter();
            SkipSpace(reader);
            char firstChar = (char)reader.Peek();
            if (firstChar == '\"' || firstChar == '\'')
            {
                return TR.Exit(JString.Parse(reader));
            }
            if (firstChar == '[')
            {
                return TR.Exit(JArray.Parse(reader));
            }
            if ((firstChar >= '0' && firstChar <= '9') || firstChar == '-')
            {
                return TR.Exit(JNumber.Parse(reader));
            }
            if (firstChar == 't' || firstChar == 'f')
            {
                return TR.Exit(JBoolean.Parse(reader));
            }
            if (firstChar == 'n')
            {
                return TR.Exit(ParseNull(reader));
            }
            if (reader.Read() != '{')
            {
                TR.Exit();
                throw new FormatException();
            }
            SkipSpace(reader);
            JObject obj = new JObject();
            while (reader.Peek() != '}')
            {
                if (reader.Peek() == ',') reader.Read();
                SkipSpace(reader);
                string name = JString.Parse(reader).Value;
                SkipSpace(reader);
                if (reader.Read() != ':')
                {
                    TR.Exit();
                    throw new FormatException();
                }
                JObject value = Parse(reader);
                obj.properties.Add(name, value);
                SkipSpace(reader);
            }
            reader.Read();
            return TR.Exit(obj);
        }

        public static JObject Parse(string value)
        {
            TR.Enter();
            using (StringReader reader = new StringReader(value))
            {
                return TR.Exit(Parse(reader));
            }
        }

        private static JObject ParseNull(TextReader reader)
        {
            TR.Enter();
            char firstChar = (char)reader.Read();
            if (firstChar == 'n')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                if (c2 == 'u' && c3 == 'l' && c4 == 'l')
                {
                    TR.Exit();
                    return null;
                }
            }
            TR.Exit();
            throw new FormatException();
        }

        protected static void SkipSpace(TextReader reader)
        {
            TR.Enter();
            while (reader.Peek() == ' ' || reader.Peek() == '\t' || reader.Peek() == '\r' || reader.Peek() == '\n')
            {
                reader.Read();
            }
            TR.Exit();
        }

        public override string ToString()
        {
            TR.Enter();
            StringBuilder sb = new StringBuilder();
            sb.Append('{');
            foreach (KeyValuePair<string, JObject> pair in properties)
            {
                sb.Append('"');
                sb.Append(pair.Key);
                sb.Append('"');
                sb.Append(':');
                if (pair.Value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append(pair.Value);
                }
                sb.Append(',');
            }
            if (properties.Count == 0)
            {
                sb.Append('}');
            }
            else
            {
                sb[sb.Length - 1] = '}';
            }
            return TR.Exit(sb.ToString());
        }

        public static implicit operator JObject(Enum value)
        {
            TR.Enter();
            return TR.Exit(new JString(value.ToString()));
        }

        public static implicit operator JObject(JObject[] value)
        {
            TR.Enter();
            return TR.Exit(new JArray(value));
        }

        public static implicit operator JObject(bool value)
        {
            TR.Enter();
            return TR.Exit(new JBoolean(value));
        }

        public static implicit operator JObject(double value)
        {
            TR.Enter();
            return TR.Exit(new JNumber(value));
        }

        public static implicit operator JObject(string value)
        {
            TR.Enter();
            return TR.Exit(value == null ? null : new JString(value));
        }
    }
}
