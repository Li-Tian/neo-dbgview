using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DbgViewTR
{
#if DEBUG
    class DbgData
    {
        public string file_name;
        public int line_number;
        public int column_number;
        public string method_name;
        public string method_full_name;
        public int thread_id;
        public int? task_id;
        public DbgData(StackFrame sf)
        {
            file_name = sf?.GetFileName();
            line_number = sf.GetFileLineNumber();
            column_number = sf.GetFileColumnNumber();
            if (file_name != null)
            {
                int slash_location = file_name.LastIndexOf('\\');
                if (slash_location > 0)
                {
                    file_name = file_name.Substring(slash_location + 1);
                }
            }
            MethodBase mb = sf.GetMethod();
            method_name = mb.Name;
            method_full_name = mb.ReflectedType.ToString() + "#" + mb.ToString();
            thread_id = Thread.CurrentThread.ManagedThreadId;
            task_id = Task.CurrentId;
        }

        public IndentKey GetIndentKey()
        {
            return new IndentKey(thread_id, task_id);
        }
    }

    class IndentUnit
    {
        private int indent = 1;
        public void Indent()
        {
            lock(this)
            {
                indent += 2;
            }
        }
        public bool Unindent()
        {
            lock(this)
            {
                if (indent > 1)
                {
                    indent -= 2;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public string Get()
        {
            int indent_;
            lock(this)
            {
                indent_ = indent;
            }
            return "".PadRight(indent_);
        }
    }

    class IndentKey
    {
        private int thread_id;
        private int? task_id;
        public IndentKey(int threadId, int? taskId)
        {
            thread_id = threadId;
            task_id = taskId;
        }
        public string GetKey()
        {
            if (task_id != null)
            {
                return "TASK_" + task_id;
            }
            return "Thread_" + thread_id;
        }
    }

    class IndentManager
    {
        private static IndentManager instance = null;
        private static object locker = new object();
        public static IndentManager getInstance()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new IndentManager();
                    }
                }
            }
            return instance;
        }
        private Dictionary<string, IndentUnit> indentDictionary = new Dictionary<string, IndentUnit>();
        public IndentUnit Get(IndentKey key)
        {
            string keyStr = key.GetKey();
            lock(locker)
            {
                if (indentDictionary.ContainsKey(keyStr))
                {
                    return indentDictionary[keyStr];
                }
                else
                {
                    IndentUnit iu = new IndentUnit();
                    indentDictionary.Add(keyStr, iu);
                    return iu;
                }
            }
        }
    }
#endif

    class TR
    {
#if DEBUG
        //[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        //public static extern void OutputDebugString(string message);

        const string project_key = "NEO-DBG";
#endif

        public static void Enter()
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, ">");
#endif
        }

        public static void Exit()
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, "<");
#endif
        }

        public static T Exit<T>(T result)
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, "return {0}", result?.ToString());
            Log(dd, "<");
#endif
            return result;
        }

        public static void Log(string format, params object[] args)
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, format, args);
#endif
        }

        public static void Log()
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, "");
#endif
        }

        public static T Log<T>(T obj)
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, "{0}", obj?.ToString());
#endif
            return obj;
        }

#if DEBUG
        private static void Log(DbgData dd, string format, params object[] args)
        {
            IndentKey ik = dd.GetIndentKey();
            IndentUnit iu = IndentManager.getInstance().Get(ik);
            if (format == "<")
            {
                if (!iu.Unindent())
                {
                    Debug.WriteLine(String.Format("[{0}]Warning : log indent error", project_key));
                }
            }
            string indentStr = iu.Get();
            string dbgStr;
            if (dd.file_name == null)
            {
                dbgStr = String.Format("[{0}][{1}]<{2}>{3}{4}", project_key, dd.thread_id, (dd.task_id?.ToString()??"-"), indentStr, dd.method_full_name);
            }
            else
            {
                dbgStr = String.Format("[{0}][{1}]<{2}>{3}{4}({5}){6}", project_key, dd.thread_id, (dd.task_id?.ToString() ?? "-"), indentStr, dd.file_name, dd.line_number, dd.method_name);
            }
            string logStr = String.Format(format, args);
            string finalStr = String.Format("{0} : {1}", dbgStr, logStr);
            //Console.WriteLine(finalStr);
            Debug.WriteLine(finalStr);
            //Debugger.Log(0, null, finalStr);
            //OutputDebugString(finalStr);
            if (format == ">")
            {
                iu.Indent();
            }
        }
#endif
    }
}
