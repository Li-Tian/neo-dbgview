using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class ReadOptions
    {
        public static readonly ReadOptions Default = new ReadOptions();
        internal readonly IntPtr handle = Native.leveldb_readoptions_create();

        public bool VerifyChecksums
        {
            set
            {
                TR.Log(value);
                Native.leveldb_readoptions_set_verify_checksums(handle, value);
            }
        }

        public bool FillCache
        {
            set
            {
                TR.Log(value);
                Native.leveldb_readoptions_set_fill_cache(handle, value);
            }
        }

        public Snapshot Snapshot
        {
            set
            {
                TR.Log(value);
                Native.leveldb_readoptions_set_snapshot(handle, value.handle);
            }
        }

        ~ReadOptions()
        {
            TR.Log(handle);
            Native.leveldb_readoptions_destroy(handle);
        }
    }
}
