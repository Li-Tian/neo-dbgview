using Neo.Core;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Network.Payloads;
using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;
using DbgViewTR;

namespace Neo.Consensus
{
    internal class ConsensusContext
    {
        public const uint Version = 0;
        public ConsensusState State;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public byte ViewNumber;
        public ECPoint[] Validators;
        public int MyIndex;
        public uint PrimaryIndex;
        public uint Timestamp;
        public ulong Nonce;
        public UInt160 NextConsensus;
        public UInt256[] TransactionHashes;
        public Dictionary<UInt256, Transaction> Transactions;
        public byte[][] Signatures;
        public byte[] ExpectedView;
        public KeyPair KeyPair;

        public int M => Validators.Length - (Validators.Length - 1) / 3;

        public void ChangeView(byte view_number)
        {
            TR.Enter();
            int p = ((int)BlockIndex - view_number) % Validators.Length;
            State &= ConsensusState.SignatureSent;
            ViewNumber = view_number;
            PrimaryIndex = p >= 0 ? (uint)p : (uint)(p + Validators.Length);
            if (State == ConsensusState.Initial)
            {
                TransactionHashes = null;
                Signatures = new byte[Validators.Length][];
            }
            if (MyIndex >= 0)
                ExpectedView[MyIndex] = view_number;
            _header = null;
            TR.Exit();
        }

        public ConsensusPayload MakeChangeView()
        {
            TR.Enter();
            return TR.Exit(MakePayload(new ChangeView
            {
                NewViewNumber = ExpectedView[MyIndex]
            }));
        }

        private Block _header = null;
        public Block MakeHeader()
        {
            TR.Enter();
            if (TransactionHashes == null) return null;
            if (_header == null)
            {
                _header = new Block
                {
                    Version = Version,
                    PrevHash = PrevHash,
                    MerkleRoot = MerkleTree.ComputeRoot(TransactionHashes),
                    Timestamp = Timestamp,
                    Index = BlockIndex,
                    ConsensusData = Nonce,
                    NextConsensus = NextConsensus,
                    Transactions = new Transaction[0]
                };
            }
            return TR.Exit(_header);
        }

        private ConsensusPayload MakePayload(ConsensusMessage message)
        {
            TR.Enter();
            message.ViewNumber = ViewNumber;
            return TR.Exit(new ConsensusPayload
            {
                Version = Version,
                PrevHash = PrevHash,
                BlockIndex = BlockIndex,
                ValidatorIndex = (ushort)MyIndex,
                Timestamp = Timestamp,
                Data = message.ToArray()
            });
        }

        public ConsensusPayload MakePrepareRequest()
        {
            TR.Enter();
            return TR.Exit(MakePayload(new PrepareRequest
            {
                Nonce = Nonce,
                NextConsensus = NextConsensus,
                TransactionHashes = TransactionHashes,
                MinerTransaction = (MinerTransaction)Transactions[TransactionHashes[0]],
                Signature = Signatures[MyIndex]
            }));
        }

        public ConsensusPayload MakePrepareResponse(byte[] signature)
        {
            TR.Enter();
            return TR.Exit(MakePayload(new PrepareResponse
            {
                Signature = signature
            }));
        }

        public void Reset(Wallet wallet)
        {
            TR.Enter();
            State = ConsensusState.Initial;
            PrevHash = Blockchain.Default.CurrentBlockHash;
            BlockIndex = Blockchain.Default.Height + 1;
            ViewNumber = 0;
            Validators = Blockchain.Default.GetValidators();
            MyIndex = -1;
            PrimaryIndex = BlockIndex % (uint)Validators.Length;
            TransactionHashes = null;
            Signatures = new byte[Validators.Length][];
            ExpectedView = new byte[Validators.Length];
            KeyPair = null;

            for (int i = 0; i < Validators.Length; i++)
            {
                WalletAccount account = wallet.GetAccount(Validators[i]);
                if (account?.HasKey == true)
                {
                    MyIndex = i;
                    KeyPair = account.GetKey();
                    break;
                }
            }
            _header = null;
            TR.Exit();
        }
    }
}
