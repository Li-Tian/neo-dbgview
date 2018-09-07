using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class DB : IDisposable
    {
        private IntPtr handle;

        /// <summary>
        /// Return true if haven't got valid handle
        /// </summary>
        public bool IsDisposed => handle == IntPtr.Zero;

        private DB(IntPtr handle)
        {
            TR.Log(handle);
            this.handle = handle;
        }

        public void Dispose()
        {
            TR.Enter();
            if (handle != IntPtr.Zero)
            {
                TR.Log(handle);
                Native.leveldb_close(handle);
                handle = IntPtr.Zero;
            }
            TR.Exit();
        }

        public void Delete(WriteOptions options, Slice key)
        {
            TR.Enter();
            IntPtr error;
            Native.leveldb_delete(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out error);
            NativeHelper.CheckError(error);
            TR.Exit();
        }

        public Slice Get(ReadOptions options, Slice key)
        {
            TR.Enter();
            UIntPtr length;
            IntPtr error;
            IntPtr value = Native.leveldb_get(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
            try
            {
                NativeHelper.CheckError(error);
                if (value == IntPtr.Zero)
                {
                    TR.Exit();
                    throw new LevelDBException("not found");
                }
                return TR.Exit(new Slice(value, length));
            }
            finally
            {
                TR.Log(value);
                if (value != IntPtr.Zero) Native.leveldb_free(value);
            }
        }

        public Snapshot GetSnapshot()
        {
            return TR.Log(new Snapshot(handle));
        }

        public Iterator NewIterator(ReadOptions options)
        {
            return TR.Log(new Iterator(Native.leveldb_create_iterator(handle, options.handle)));
        }

        public static DB Open(string name)
        {
            return TR.Log(Open(name, Options.Default));
        }

        public static DB Open(string name, Options options)
        {
            TR.Enter();
            IntPtr error;
            IntPtr handle = Native.leveldb_open(options.handle, name, out error);
            NativeHelper.CheckError(error);
            return TR.Exit(new DB(handle));
        }

        public void Put(WriteOptions options, Slice key, Slice value)
        {
            TR.Enter();
            IntPtr error;
            Native.leveldb_put(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length, out error);
            NativeHelper.CheckError(error);
            TR.Exit();
        }

        public bool TryGet(ReadOptions options, Slice key, out Slice value)
        {
            TR.Enter();
            UIntPtr length;
            IntPtr error;
            IntPtr v = Native.leveldb_get(handle, options.handle, key.buffer, (UIntPtr)key.buffer.Length, out length, out error);
            if (error != IntPtr.Zero)
            {
                Native.leveldb_free(error);
                value = default(Slice);
                return TR.Exit(false);
            }
            if (v == IntPtr.Zero)
            {
                value = default(Slice);
                return TR.Exit(false);
            }
            value = new Slice(v, length);
            Native.leveldb_free(v);
            return TR.Exit(true);
        }

        public void Write(WriteOptions options, WriteBatch write_batch)
        {
            // There's a bug in .Net Core.
            // When calling DB.Write(), it will throw LevelDBException sometimes.
            // But when you try to catch the exception, the bug disappears.
            // We shall remove the "try...catch" clause when Microsoft fix the bug.
            TR.Enter();
            byte retry = 0;
            while (true)
            {
                try
                {
                    TR.Log(retry);
                    IntPtr error;
                    Native.leveldb_write(handle, options.handle, write_batch.handle, out error);
                    NativeHelper.CheckError(error);
                    break;
                }
                catch (LevelDBException ex)
                {
                    if (++retry >= 4) throw;
                    System.IO.File.AppendAllText("leveldb.log", ex.Message + "\r\n");
                }
            }
            TR.Exit();
        }
    }
}
