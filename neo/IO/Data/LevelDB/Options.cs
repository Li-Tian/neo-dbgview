using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class Options
    {
        public static readonly Options Default = new Options();
        internal readonly IntPtr handle = Native.leveldb_options_create();

        public bool CreateIfMissing
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_create_if_missing(handle, value);
            }
        }

        public bool ErrorIfExists
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_error_if_exists(handle, value);
            }
        }

        public bool ParanoidChecks
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_paranoid_checks(handle, value);
            }
        }

        public int WriteBufferSize
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_write_buffer_size(handle, (UIntPtr)value);
            }
        }

        public int MaxOpenFiles
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_max_open_files(handle, value);
            }
        }

        public int BlockSize
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_block_size(handle, (UIntPtr)value);
            }
        }

        public int BlockRestartInterval
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_block_restart_interval(handle, value);
            }
        }

        public CompressionType Compression
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_compression(handle, value);
            }
        }

        public IntPtr FilterPolicy
        {
            set
            {
                TR.Log(value);
                Native.leveldb_options_set_filter_policy(handle, value);
            }
        }

        ~Options()
        {
            TR.Log(handle);
            Native.leveldb_options_destroy(handle);
        }
    }
}
