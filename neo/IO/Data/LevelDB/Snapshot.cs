using System;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class Snapshot : IDisposable
    {
        internal IntPtr db, handle;

        internal Snapshot(IntPtr db)
        {
            TR.Enter();
            this.db = db;
            this.handle = Native.leveldb_create_snapshot(db);
            TR.Exit();
        }

        public void Dispose()
        {
            TR.Enter();
            if (handle != IntPtr.Zero)
            {
                TR.Log();
                Native.leveldb_release_snapshot(db, handle);
                handle = IntPtr.Zero;
            }
            TR.Exit();
        }
    }
}
