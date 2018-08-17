using System;
using DbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class WriteBatch
    {
        internal readonly IntPtr handle = Native.leveldb_writebatch_create();

        ~WriteBatch()
        {
            TR.Enter();
            Native.leveldb_writebatch_destroy(handle);
            TR.Exit();
        }

        public void Clear()
        {
            TR.Enter();
            Native.leveldb_writebatch_clear(handle);
            TR.Exit();
        }

        public void Delete(Slice key)
        {
            TR.Enter();
            Native.leveldb_writebatch_delete(handle, key.buffer, (UIntPtr)key.buffer.Length);
            TR.Exit();
        }

        public void Put(Slice key, Slice value)
        {
            TR.Enter();
            Native.leveldb_writebatch_put(handle, key.buffer, (UIntPtr)key.buffer.Length, value.buffer, (UIntPtr)value.buffer.Length);
            TR.Exit();
        }
    }
}
