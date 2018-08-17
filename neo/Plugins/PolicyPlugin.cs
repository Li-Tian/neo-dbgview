using Neo.Core;
using System.Collections.Generic;
using DbgViewTR;

namespace Neo.Plugins
{
    public abstract class PolicyPlugin : Plugin
    {
        private static readonly List<PolicyPlugin> instances = new List<PolicyPlugin>();

        public new static IEnumerable<PolicyPlugin> Instances => instances;

        protected PolicyPlugin()
        {
            TR.Enter();
            instances.Add(this);
            TR.Exit();
        }

        internal protected virtual bool CheckPolicy(Transaction tx) => true;
        internal protected virtual IEnumerable<Transaction> Filter(IEnumerable<Transaction> transactions) => transactions;
    }
}
