using Neo.IO;
using System.IO;
using System.Linq;
using DbgViewTR;

namespace Neo.Core
{
    public class UnspentCoinState : StateBase, ICloneable<UnspentCoinState>
    {
        public CoinState[] Items;

        public override int Size => base.Size + Items.GetVarSize();

        UnspentCoinState ICloneable<UnspentCoinState>.Clone()
        {
            TR.Enter();
            return TR.Exit(new UnspentCoinState
            {
                Items = (CoinState[])Items.Clone()
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            Items = reader.ReadVarBytes().Select(p => (CoinState)p).ToArray();
            TR.Exit();
        }

        void ICloneable<UnspentCoinState>.FromReplica(UnspentCoinState replica)
        {
            TR.Enter();
            Items = replica.Items;
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.WriteVarBytes(Items.Cast<byte>().ToArray());
            TR.Exit();
        }
    }
}
