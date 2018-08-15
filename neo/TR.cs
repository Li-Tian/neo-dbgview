using Microsoft.Extensions.Configuration;
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
        public string thread_name;
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
            thread_name = Thread.CurrentThread.Name;
        }

        public IndentKey GetIndentKey()
        {
            return new IndentKey(thread_id, task_id);
        }
    }

    public class IndentContext
    {
        private int indent = 1;
        private int context_id = 0;

        public IndentContext()
        {
            indent = 1;
            ShuffleContext();
        }

        public IndentContext(IndentContext iu)
        {
            this.indent = iu.indent;
            this.context_id = iu.context_id;
        }

        public void ShuffleContext()
        {
            context_id = IndentManager.GetInstance().GetNextContextId();
        }

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
        public string GetIndent()
        {
            int indent_;
            lock(this)
            {
                indent_ = indent;
            }
            return "".PadRight(indent_);
        }

        public int GetContextId()
        {
            return context_id;
        }
    }

    class IndentKey
    {
        private readonly int thread_id;
        private readonly int? task_id;
        public IndentKey(int threadId, int? taskId)
        {
            thread_id = threadId;
            task_id = taskId;
        }
        public string GetKey()
        {
            //if (task_id != null)
            //{
            //    return "TASK_" + task_id;
            //}
            return "Thread_" + thread_id;
        }
    }

    class IndentManager
    {
        private static IndentManager instance = null;
        private static readonly object locker = new object();
        public static IndentManager GetInstance()
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
        private Dictionary<string, IndentContext> indentDictionary = new Dictionary<string, IndentContext>();
        private int ContextIdSeq = 1;
        public int GetNextContextId()
        {
            lock(locker)
            {
                return ContextIdSeq++;
            }
        }
        public IndentContext Get(IndentKey key)
        {
            string keyStr = key.GetKey();
            lock (locker)
            {
                if (indentDictionary.ContainsKey(keyStr))
                {
                    return indentDictionary[keyStr];
                }
                else
                {
                    IndentContext iu = new IndentContext();
                    indentDictionary.Add(keyStr, iu);
                    return iu;
                }
            }
        }
        public void Put(IndentKey key, IndentContext iu)
        {
            lock (locker)
            {
                indentDictionary[key.GetKey()] = iu;
            }
        }
    }
#else
    public class IndentContext
    {
    }
#endif

    class TR
    {
#if DEBUG
        //[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        //public static extern void OutputDebugString(string message);

        static readonly string project_key = "DBG-" + Assembly.GetExecutingAssembly().GetName().Name;
#endif

        public static IndentContext SaveContextAndShuffle()
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            Log(dd, "SaveContextAndShuffle()");
            IndentKey ik = dd.GetIndentKey();
            IndentContext iu = IndentManager.GetInstance().Get(ik);
            IndentContext copy = new IndentContext(iu);
            iu.ShuffleContext();
            return copy;
#else
            return null;
#endif
        }

        public static void RestoreContext(IndentContext iu)
        {
#if DEBUG
            StackFrame sf = new StackFrame(1, true);
            DbgData dd = new DbgData(sf);
            IndentKey ik = dd.GetIndentKey();
            IndentManager.GetInstance().Put(ik, iu);
            Log(dd, "RestoreContext()");
#endif
        }

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
            var ignoreFiles = GetIgnoreFileSet();
            if (ignoreFiles.Contains(dd.file_name?.ToLower()))
            {
                return;
            }
            IndentKey ik = dd.GetIndentKey();
            IndentContext iu = IndentManager.GetInstance().Get(ik);
            if (format == "<")
            {
                if (!iu.Unindent())
                {
                    Debug.WriteLine(String.Format("[{0}][{1}]<{2}>Warning : log indent error", project_key, dd.thread_id, iu.GetContextId()));
                }
            }
            string indentStr = iu.GetIndent();
            string dbgStr;
            if (dd.file_name == null)
            {
                dbgStr = String.Format("[{0}][{1}]<{2}>{3}{4}[{5}]", project_key, dd.thread_id, iu.GetContextId(), indentStr, dd.method_full_name, dd.thread_name);
            }
            else
            {
                dbgStr = String.Format("[{0}][{1}]<{2}>{3}{4}({5}){6}[{7}]", project_key, dd.thread_id, iu.GetContextId(), indentStr, dd.file_name, dd.line_number, dd.method_name, dd.thread_name);
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

        private static HashSet<string> ignoreFiles = null;
        private static readonly Object locker = new Object();

        private static HashSet<string> GetIgnoreFileSet()
        {
            if (ignoreFiles == null)
            {
                lock (locker)
                {
                    if (ignoreFiles == null)
                    {
                        ignoreFiles = new HashSet<string>();
                        try
                        {
                            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("dbgview_setting.json").Build().GetSection("DbgViewSetting");
                            ignoreFiles.UnionWith(section.GetSection("IgnoreFiles").GetChildren().Select(p => p.Value.ToLower()).ToList());
                            Debug.Print("[{0}] Ignore files loaded : {1}", project_key, ignoreFiles.Count);
                        }
                        catch (Exception e)
                        {
                            Debug.Print("[{0}] Exception : {1}", project_key, e.Message);
                        }
                    }
                }
            }
            return ignoreFiles;
        }
#endif
    }
}

namespace NoDbgViewTR
{
    public class IndentContext
    {
    }

    class TR
    {
        public static IndentContext SaveContextAndShuffle()
        {
            return null;
        }
        public static void RestoreContext(IndentContext iu)
        {
        }
        public static void Enter()
        {

        }
        public static void Exit()
        {
        }
        public static T Exit<T>(T result)
        {
            return result;
        }
        public static void Log(string format, params object[] args)
        {
        }
        public static void Log()
        {
        }
        public static T Log<T>(T obj)
        {
            return obj;
        }
    }
}