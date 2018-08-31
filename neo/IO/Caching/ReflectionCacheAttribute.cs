using System;
using NoDbgViewTR;

namespace Neo.IO.Caching
{
    public class ReflectionCacheAttribute : Attribute
    {
        /// <summary>
        /// Type
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        public ReflectionCacheAttribute(Type type)
        {
            TR.Enter();
            Type = type;
            TR.Exit();
        }
    }
}