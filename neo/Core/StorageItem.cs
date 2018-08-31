using Neo.IO;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public class StorageItem : StateBase, ICloneable<StorageItem>
    {
        public byte[] Value;

        public override int Size => base.Size + Value.GetVarSize();

        StorageItem ICloneable<StorageItem>.Clone()
        {
            TR.Enter();
            return TR.Exit(new StorageItem
            {
                Value = Value
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            Value = reader.ReadVarBytes();
            TR.Exit();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            TR.Enter();
            Value = replica.Value;
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
            TR.Exit();
        }
    }
}
