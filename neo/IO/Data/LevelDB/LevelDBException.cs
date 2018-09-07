using System.Data.Common;
using NoDbgViewTR;

namespace Neo.IO.Data.LevelDB
{
    public class LevelDBException : DbException
    {
        internal LevelDBException(string message)
            : base(message)
        {
            TR.Log(message);
        }
    }
}
