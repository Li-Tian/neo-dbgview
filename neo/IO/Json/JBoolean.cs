using System;
using System.IO;
using DbgViewTR;

namespace Neo.IO.Json
{
    public class JBoolean : JObject
    {
        public bool Value { get; private set; }

        public JBoolean(bool value = false)
        {
            TR.Enter();
            this.Value = value;
            TR.Exit();
        }

        public override bool AsBoolean()
        {
            TR.Enter();
            return TR.Exit(Value);
        }

        public override string AsString()
        {
            TR.Enter();
            return TR.Exit(Value.ToString().ToLower());
        }

        public override bool CanConvertTo(Type type)
        {
            TR.Enter();
            if (type == typeof(bool))
                return TR.Exit(true);
            if (type == typeof(string))
                return TR.Exit(true);
            return TR.Exit(false);
        }

        internal new static JBoolean Parse(TextReader reader)
        {
            TR.Enter();
            SkipSpace(reader);
            char firstChar = (char)reader.Read();
            if (firstChar == 't')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                if (c2 == 'r' && c3 == 'u' && c4 == 'e')
                {
                    return TR.Exit(new JBoolean(true));
                }
            }
            else if (firstChar == 'f')
            {
                int c2 = reader.Read();
                int c3 = reader.Read();
                int c4 = reader.Read();
                int c5 = reader.Read();
                if (c2 == 'a' && c3 == 'l' && c4 == 's' && c5 == 'e')
                {
                    return TR.Exit(new JBoolean(false));
                }
            }
            TR.Exit();
            throw new FormatException();
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(Value.ToString().ToLower());
        }
    }
}
