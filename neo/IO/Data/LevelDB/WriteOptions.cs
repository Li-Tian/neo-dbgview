using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class WriteOptions
    {
        public static readonly WriteOptions Default = new WriteOptions();
        internal readonly IntPtr handle = Native.leveldb_writeoptions_create();

        public bool Sync
        {
            set
            {
                TR.Log(value);
                Native.leveldb_writeoptions_set_sync(handle, value);
            }
        }

        ~WriteOptions()
        {
            TR.Log(handle);
            Native.leveldb_writeoptions_destroy(handle);
        }
    }
}
