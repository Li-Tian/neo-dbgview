using DbgViewTR;
using System.ComponentModel;

namespace Neo.Network
{
    public class InventoryReceivingEventArgs : CancelEventArgs
    {
        public IInventory Inventory { get; }

        public InventoryReceivingEventArgs(IInventory inventory)
        {
            TR.Enter();
            this.Inventory = inventory;
            TR.Exit();
        }
    }
}
