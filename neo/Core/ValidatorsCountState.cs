using Neo.IO;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public class ValidatorsCountState : StateBase, ICloneable<ValidatorsCountState>
    {
        public Fixed8[] Votes;

        public override int Size => base.Size + Votes.GetVarSize();

        public ValidatorsCountState()
        {
            TR.Enter();
            this.Votes = new Fixed8[Blockchain.MaxValidators];
            TR.Exit();
        }

        ValidatorsCountState ICloneable<ValidatorsCountState>.Clone()
        {
            TR.Enter();
            return TR.Exit(new ValidatorsCountState
            {
                Votes = (Fixed8[])Votes.Clone()
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            Votes = reader.ReadSerializableArray<Fixed8>();
            TR.Exit();
        }

        void ICloneable<ValidatorsCountState>.FromReplica(ValidatorsCountState replica)
        {
            TR.Enter();
            Votes = replica.Votes;
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(Votes);
            TR.Exit();
        }
    }
}
