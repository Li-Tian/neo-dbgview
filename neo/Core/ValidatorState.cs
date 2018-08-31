using Neo.Cryptography.ECC;
using Neo.IO;
using System.IO;
using NoDbgViewTR;

namespace Neo.Core
{
    public class ValidatorState : StateBase, ICloneable<ValidatorState>
    {
        public ECPoint PublicKey;
        public bool Registered;
        public Fixed8 Votes;

        public override int Size => base.Size + PublicKey.Size + sizeof(bool) + Votes.Size;

        public ValidatorState() { }

        public ValidatorState(ECPoint pubkey)
        {
            TR.Enter();
            this.PublicKey = pubkey;
            this.Registered = false;
            this.Votes = Fixed8.Zero;
            TR.Exit();
        }

        ValidatorState ICloneable<ValidatorState>.Clone()
        {
            TR.Enter();
            return TR.Exit(new ValidatorState
            {
                PublicKey = PublicKey,
                Registered = Registered,
                Votes = Votes
            });
        }

        public override void Deserialize(BinaryReader reader)
        {
            TR.Enter();
            base.Deserialize(reader);
            PublicKey = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            Registered = reader.ReadBoolean();
            Votes = reader.ReadSerializable<Fixed8>();
            TR.Exit();
        }

        void ICloneable<ValidatorState>.FromReplica(ValidatorState replica)
        {
            TR.Enter();
            PublicKey = replica.PublicKey;
            Registered = replica.Registered;
            Votes = replica.Votes;
            TR.Exit();
        }

        public override void Serialize(BinaryWriter writer)
        {
            TR.Enter();
            base.Serialize(writer);
            writer.Write(PublicKey);
            writer.Write(Registered);
            writer.Write(Votes);
            TR.Exit();
        }
    }
}
