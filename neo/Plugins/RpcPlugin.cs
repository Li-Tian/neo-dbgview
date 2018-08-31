using Microsoft.AspNetCore.Http;
using Neo.IO.Json;
using System.Collections.Generic;
using NoDbgViewTR;

namespace Neo.Plugins
{
    public abstract class RpcPlugin : Plugin
    {
        private static readonly List<RpcPlugin> instances = new List<RpcPlugin>();

        public new static IEnumerable<RpcPlugin> Instances => instances;

        protected RpcPlugin()
        {
            TR.Enter();
            instances.Add(this);
            TR.Exit();
        }

        internal protected virtual JObject OnProcess(HttpContext context, string method, JArray _params) => null;
    }
}
