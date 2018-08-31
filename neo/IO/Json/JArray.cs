using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NoDbgViewTR;

namespace Neo.IO.Json
{
    public class JArray : JObject, IList<JObject>
    {
        private List<JObject> items = new List<JObject>();

        public JArray(params JObject[] items) : this((IEnumerable<JObject>)items)
        {
        }

        public JArray(IEnumerable<JObject> items)
        {
            TR.Enter();
            this.items.AddRange(items);
            TR.Exit();
        }

        public JObject this[int index]
        {
            get
            {
                return items[index];
            }
            set
            {
                items[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(JObject item)
        {
            TR.Enter();
            items.Add(item);
            TR.Exit();
        }

        public void Clear()
        {
            TR.Enter();
            items.Clear();
            TR.Exit();
        }

        public bool Contains(JObject item)
        {
            TR.Enter();
            return TR.Exit(items.Contains(item));
        }

        public void CopyTo(JObject[] array, int arrayIndex)
        {
            TR.Enter();
            items.CopyTo(array, arrayIndex);
            TR.Exit();
        }

        public IEnumerator<JObject> GetEnumerator()
        {
            TR.Enter();
            return TR.Exit(items.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            TR.Enter();
            return TR.Exit(GetEnumerator());
        }

        public int IndexOf(JObject item)
        {
            TR.Enter();
            return TR.Exit(items.IndexOf(item));
        }

        public void Insert(int index, JObject item)
        {
            TR.Enter();
            items.Insert(index, item);
            TR.Exit();
        }

        internal new static JArray Parse(TextReader reader)
        {
            TR.Enter();
            SkipSpace(reader);
            if (reader.Read() != '[')
            {
                TR.Exit();
                throw new FormatException();
            }
            SkipSpace(reader);
            JArray array = new JArray();
            while (reader.Peek() != ']')
            {
                if (reader.Peek() == ',') reader.Read();
                JObject obj = JObject.Parse(reader);
                array.items.Add(obj);
                SkipSpace(reader);
            }
            reader.Read();
            return TR.Exit(array);
        }

        public bool Remove(JObject item)
        {
            TR.Enter();
            return TR.Exit(items.Remove(item));
        }

        public void RemoveAt(int index)
        {
            TR.Enter();
            items.RemoveAt(index);
            TR.Exit();
        }

        public override string ToString()
        {
            TR.Enter();
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach (JObject item in items)
            {
                if (item == null)
                    sb.Append("null");
                else
                    sb.Append(item);
                sb.Append(',');
            }
            if (items.Count == 0)
            {
                sb.Append(']');
            }
            else
            {
                sb[sb.Length - 1] = ']';
            }
            return TR.Exit(sb.ToString());
        }
    }
}
