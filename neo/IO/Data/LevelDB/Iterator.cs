using System;
using DbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class Iterator : IDisposable
    {
        private IntPtr handle;

        internal Iterator(IntPtr handle)
        {
            TR.Enter();
            this.handle = handle;
            TR.Exit();
        }

        private void CheckError()
        {
            TR.Enter();
            IntPtr error;
            Native.leveldb_iter_get_error(handle, out error);
            NativeHelper.CheckError(error);
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            if (handle != IntPtr.Zero)
            {
                Native.leveldb_iter_destroy(handle);
                handle = IntPtr.Zero;
            }
            TR.Exit();
        }

        public Slice Key()
        {
            TR.Enter();
            UIntPtr length;
            IntPtr key = Native.leveldb_iter_key(handle, out length);
            CheckError();
            return TR.Exit(new Slice(key, length));
        }

        public void Next()
        {
            TR.Enter();
            Native.leveldb_iter_next(handle);
            CheckError();
            TR.Exit();
        }

        public void Prev()
        {
            TR.Enter();
            Native.leveldb_iter_prev(handle);
            CheckError();
            TR.Exit();
        }

        public void Seek(Slice target)
        {
            TR.Enter();
            Native.leveldb_iter_seek(handle, target.buffer, (UIntPtr)target.buffer.Length);
            TR.Exit();
        }

        public void SeekToFirst()
        {
            TR.Enter();
            Native.leveldb_iter_seek_to_first(handle);
            TR.Exit();
        }

        public void SeekToLast()
        {
            TR.Enter();
            Native.leveldb_iter_seek_to_last(handle);
            TR.Exit();
        }

        public bool Valid()
        {
            TR.Enter();
            return TR.Exit(Native.leveldb_iter_valid(handle));
        }

        public Slice Value()
        {
            TR.Enter();
            UIntPtr length;
            IntPtr value = Native.leveldb_iter_value(handle, out length);
            CheckError();
            return TR.Exit(new Slice(value, length));
        }
    }
}
