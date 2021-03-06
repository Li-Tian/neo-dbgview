﻿using System;
using NoDbgViewTR;

namespace Neo.Network.RPC
{
    public class RpcException : Exception
    {
        public RpcException(int code, string message) : base(message)
        {
            TR.Enter();
            HResult = code;
            TR.Exit();
        }
    }
}
