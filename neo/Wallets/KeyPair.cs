using Neo.Core;
using Neo.Cryptography;
using Neo.SmartContract;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NoDbgViewTR;

namespace Neo.Wallets
{
    public class KeyPair : IEquatable<KeyPair>
    {
        public readonly byte[] PrivateKey;
        public readonly Cryptography.ECC.ECPoint PublicKey;

        public UInt160 PublicKeyHash => PublicKey.EncodePoint(true).ToScriptHash();

        public KeyPair(byte[] privateKey)
        {
            TR.Enter();
            if (privateKey.Length != 32 && privateKey.Length != 96 && privateKey.Length != 104)
            {
                TR.Exit();
                throw new ArgumentException();
            }
            this.PrivateKey = new byte[32];
            Buffer.BlockCopy(privateKey, privateKey.Length - 32, PrivateKey, 0, 32);
            if (privateKey.Length == 32)
            {
                this.PublicKey = Cryptography.ECC.ECCurve.Secp256r1.G * privateKey;
            }
            else
            {
                this.PublicKey = Cryptography.ECC.ECPoint.FromBytes(privateKey, Cryptography.ECC.ECCurve.Secp256r1);
            }
            TR.Exit();
#if NET47
            ProtectedMemory.Protect(PrivateKey, MemoryProtectionScope.SameProcess);
#endif
        }

        public IDisposable Decrypt()
        {
            TR.Enter();
#if NET47
            return new ProtectedMemoryContext(PrivateKey, MemoryProtectionScope.SameProcess);
#else
            return TR.Exit(new System.IO.MemoryStream(0));
#endif
        }

        public bool Equals(KeyPair other)
        {
            TR.Enter();
            if (ReferenceEquals(this, other)) return TR.Exit(true);
            if (ReferenceEquals(null, other)) return TR.Exit(false);
            return TR.Exit(PublicKey.Equals(other.PublicKey));
        }

        public override bool Equals(object obj)
        {
            TR.Enter();
            return TR.Exit(Equals(obj as KeyPair));
        }

        public string Export()
        {
            TR.Enter();
            using (Decrypt())
            {
                byte[] data = new byte[34];
                data[0] = 0x80;
                Buffer.BlockCopy(PrivateKey, 0, data, 1, 32);
                data[33] = 0x01;
                string wif = data.Base58CheckEncode();
                Array.Clear(data, 0, data.Length);
                return TR.Exit(wif);
            }
        }

        public string Export(string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            TR.Enter();
            using (Decrypt())
            {
                UInt160 script_hash = Contract.CreateSignatureRedeemScript(PublicKey).ToScriptHash();
                string address = Wallet.ToAddress(script_hash);
                byte[] addresshash = Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).ToArray();
                byte[] derivedkey = SCrypt.DeriveKey(Encoding.UTF8.GetBytes(passphrase), addresshash, N, r, p, 64);
                byte[] derivedhalf1 = derivedkey.Take(32).ToArray();
                byte[] derivedhalf2 = derivedkey.Skip(32).ToArray();
                byte[] encryptedkey = XOR(PrivateKey, derivedhalf1).AES256Encrypt(derivedhalf2);
                byte[] buffer = new byte[39];
                buffer[0] = 0x01;
                buffer[1] = 0x42;
                buffer[2] = 0xe0;
                Buffer.BlockCopy(addresshash, 0, buffer, 3, addresshash.Length);
                Buffer.BlockCopy(encryptedkey, 0, buffer, 7, encryptedkey.Length);
                return TR.Exit(buffer.Base58CheckEncode());
            }
        }

        public override int GetHashCode()
        {
            TR.Enter();
            return TR.Exit(PublicKey.GetHashCode());
        }

        public override string ToString()
        {
            TR.Enter();
            return TR.Exit(PublicKey.ToString());
        }

        private static byte[] XOR(byte[] x, byte[] y)
        {
            TR.Enter();
            if (x.Length != y.Length)
            {
                TR.Exit();
                throw new ArgumentException();
            }
            return TR.Exit(x.Zip(y, (a, b) => (byte)(a ^ b)).ToArray());
        }
    }
}
