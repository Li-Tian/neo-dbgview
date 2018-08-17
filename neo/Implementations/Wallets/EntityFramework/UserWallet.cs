﻿using Microsoft.EntityFrameworkCore;
using Neo.Core;
using Neo.Cryptography;
using Neo.IO;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using DbgViewTR;

namespace Neo.Implementations.Wallets.EntityFramework
{
    public class UserWallet : Wallet, IDisposable
    {
        public override event EventHandler<BalanceEventArgs> BalanceChanged;

        private readonly string path;
        private readonly byte[] iv;
        private readonly byte[] masterKey;
        private readonly Dictionary<UInt160, UserWalletAccount> accounts;
        private readonly Dictionary<UInt256, Transaction> unconfirmed = new Dictionary<UInt256, Transaction>();

        public override string Name => Path.GetFileNameWithoutExtension(path);
        public override uint WalletHeight => WalletIndexer.IndexHeight;

        public override Version Version
        {
            get
            {
                byte[] buffer = LoadStoredData("Version");
                if (buffer == null) return new Version(0, 0);
                int major = buffer.ToInt32(0);
                int minor = buffer.ToInt32(4);
                int build = buffer.ToInt32(8);
                int revision = buffer.ToInt32(12);
                return new Version(major, minor, build, revision);
            }
        }

        private UserWallet(string path, byte[] passwordKey, bool create)
        {
            TR.Enter();
            this.path = path;
            if (create)
            {
                this.iv = new byte[16];
                this.masterKey = new byte[32];
                this.accounts = new Dictionary<UInt160, UserWalletAccount>();
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                    rng.GetBytes(masterKey);
                }
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                BuildDatabase();
                SaveStoredData("PasswordHash", passwordKey.Sha256());
                SaveStoredData("IV", iv);
                SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                SaveStoredData("Version", new[] { version.Major, version.Minor, version.Build, version.Revision }.Select(p => BitConverter.GetBytes(p)).SelectMany(p => p).ToArray());
#if NET47
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
            }
            else
            {
                byte[] passwordHash = LoadStoredData("PasswordHash");
                if (passwordHash != null && !passwordHash.SequenceEqual(passwordKey.Sha256()))
                {
                    TR.Exit();
                    throw new CryptographicException();
                }
                this.iv = LoadStoredData("IV");
                this.masterKey = LoadStoredData("MasterKey").AesDecrypt(passwordKey, iv);
#if NET47
                ProtectedMemory.Protect(masterKey, MemoryProtectionScope.SameProcess);
#endif
                this.accounts = LoadAccounts();
                WalletIndexer.RegisterAccounts(accounts.Keys);
            }
            WalletIndexer.BalanceChanged += WalletIndexer_BalanceChanged;
            TR.Exit();
        }

        private void AddAccount(UserWalletAccount account, bool is_import)
        {
            TR.Enter();
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out UserWalletAccount account_old))
                {
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                }
                else
                {
                    WalletIndexer.RegisterAccounts(new[] { account.ScriptHash }, is_import ? 0 : Blockchain.Default?.Height ?? 0);
                }
                accounts[account.ScriptHash] = account;
            }
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                if (account.HasKey)
                {
                    byte[] decryptedPrivateKey = new byte[96];
                    Buffer.BlockCopy(account.Key.PublicKey.EncodePoint(false), 1, decryptedPrivateKey, 0, 64);
                    using (account.Key.Decrypt())
                    {
                        Buffer.BlockCopy(account.Key.PrivateKey, 0, decryptedPrivateKey, 64, 32);
                    }
                    byte[] encryptedPrivateKey = EncryptPrivateKey(decryptedPrivateKey);
                    Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
                    Account db_account = ctx.Accounts.FirstOrDefault(p => p.PublicKeyHash.SequenceEqual(account.Key.PublicKeyHash.ToArray()));
                    if (db_account == null)
                    {
                        db_account = ctx.Accounts.Add(new Account
                        {
                            PrivateKeyEncrypted = encryptedPrivateKey,
                            PublicKeyHash = account.Key.PublicKeyHash.ToArray()
                        }).Entity;
                    }
                    else
                    {
                        db_account.PrivateKeyEncrypted = encryptedPrivateKey;
                    }
                }
                if (account.Contract != null)
                {
                    Contract db_contract = ctx.Contracts.FirstOrDefault(p => p.ScriptHash.SequenceEqual(account.Contract.ScriptHash.ToArray()));
                    if (db_contract != null)
                    {
                        db_contract.PublicKeyHash = account.Key.PublicKeyHash.ToArray();
                    }
                    else
                    {
                        ctx.Contracts.Add(new Contract
                        {
                            RawData = ((VerificationContract)account.Contract).ToArray(),
                            ScriptHash = account.Contract.ScriptHash.ToArray(),
                            PublicKeyHash = account.Key.PublicKeyHash.ToArray()
                        });
                    }
                }
                //add address
                {
                    Address db_address = ctx.Addresses.FirstOrDefault(p => p.ScriptHash.SequenceEqual(account.Contract.ScriptHash.ToArray()));
                    if (db_address == null)
                    {
                        ctx.Addresses.Add(new Address
                        {
                            ScriptHash = account.Contract.ScriptHash.ToArray()
                        });
                    }
                }
                ctx.SaveChanges();
            }
            TR.Exit();
        }

        public override void ApplyTransaction(Transaction tx)
        {
            TR.Enter();
            lock (unconfirmed)
            {
                unconfirmed[tx.Hash] = tx;
            }
            BalanceChanged?.Invoke(this, new BalanceEventArgs
            {
                Transaction = tx,
                RelatedAccounts = tx.Scripts.Select(p => p.ScriptHash).Union(tx.Outputs.Select(p => p.ScriptHash)).Where(p => Contains(p)).ToArray(),
                Height = null,
                Time = DateTime.UtcNow.ToTimestamp()
            });
            TR.Exit();
        }

        private void BuildDatabase()
        {
            TR.Enter();
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }
            TR.Exit();
        }

        public bool ChangePassword(string password_old, string password_new)
        {
            TR.Enter();
            if (!VerifyPassword(password_old)) return TR.Exit(false);
            byte[] passwordKey = password_new.ToAesKey();
#if NET47
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                try
                {
                    SaveStoredData("PasswordHash", passwordKey.Sha256());
                    SaveStoredData("MasterKey", masterKey.AesEncrypt(passwordKey, iv));
                    return TR.Exit(true);
                }
                finally
                {
                    Array.Clear(passwordKey, 0, passwordKey.Length);
                }
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            TR.Enter();
            lock (accounts)
            {
                return TR.Exit(accounts.ContainsKey(scriptHash));
            }
        }

        public static UserWallet Create(string path, string password)
        {
            TR.Enter();
            return TR.Exit(new UserWallet(path, password.ToAesKey(), true));
        }

        public static UserWallet Create(string path, SecureString password)
        {
            TR.Enter();
            return TR.Exit(new UserWallet(path, password.ToAesKey(), true));
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            TR.Enter();
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract = new VerificationContract
            {
                Script = SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            UserWalletAccount account = new UserWalletAccount(contract.ScriptHash)
            {
                Key = key,
                Contract = contract
            };
            AddAccount(account, false);
            return TR.Exit(account);
        }

        public override WalletAccount CreateAccount(SmartContract.Contract contract, KeyPair key = null)
        {
            TR.Enter();
            VerificationContract verification_contract = contract as VerificationContract;
            if (verification_contract == null)
            {
                verification_contract = new VerificationContract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList
                };
            }
            UserWalletAccount account = new UserWalletAccount(verification_contract.ScriptHash)
            {
                Key = key,
                Contract = verification_contract
            };
            AddAccount(account, false);
            return TR.Exit(account);
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            TR.Enter();
            UserWalletAccount account = new UserWalletAccount(scriptHash);
            AddAccount(account, true);
            return TR.Exit(account);
        }

        private byte[] DecryptPrivateKey(byte[] encryptedPrivateKey)
        {
            TR.Enter();
            if (encryptedPrivateKey == null)
            {
                TR.Exit();
                throw new ArgumentNullException(nameof(encryptedPrivateKey));
            }
            if (encryptedPrivateKey.Length != 96)
            {
                TR.Exit();
                throw new ArgumentException();
            }
#if NET47
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                return TR.Exit(encryptedPrivateKey.AesDecrypt(masterKey, iv));
            }
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            TR.Enter();
            UserWalletAccount account;
            lock (accounts)
            {
                if (accounts.TryGetValue(scriptHash, out account))
                    accounts.Remove(scriptHash);
            }
            if (account != null)
            {
                WalletIndexer.UnregisterAccounts(new[] { scriptHash });
                using (WalletDataContext ctx = new WalletDataContext(path))
                {
                    if (account.HasKey)
                    {
                        Account db_account = ctx.Accounts.First(p => p.PublicKeyHash.SequenceEqual(account.Key.PublicKeyHash.ToArray()));
                        ctx.Accounts.Remove(db_account);
                    }
                    if (account.Contract != null)
                    {
                        Contract db_contract = ctx.Contracts.First(p => p.ScriptHash.SequenceEqual(scriptHash.ToArray()));
                        ctx.Contracts.Remove(db_contract);
                    }
                    //delete address
                    {
                        Address db_address = ctx.Addresses.First(p => p.ScriptHash.SequenceEqual(scriptHash.ToArray()));
                        ctx.Addresses.Remove(db_address);
                    }
                    ctx.SaveChanges();
                }
                return TR.Exit(true);
            }
            return TR.Exit(false);
        }

        public void Dispose()
        {
            TR.Enter();
            WalletIndexer.BalanceChanged -= WalletIndexer_BalanceChanged;
            TR.Exit();
        }

        private byte[] EncryptPrivateKey(byte[] decryptedPrivateKey)
        {
            TR.Enter();
#if NET47
            using (new ProtectedMemoryContext(masterKey, MemoryProtectionScope.SameProcess))
#endif
            {
                return TR.Exit(decryptedPrivateKey.AesEncrypt(masterKey, iv));
            }
        }

        public override Coin[] FindUnspentCoins(UInt256 asset_id, Fixed8 amount, UInt160[] from)
        {
            TR.Enter();
            return TR.Exit(FindUnspentCoins(FindUnspentCoins(from).ToArray().Where(p => GetAccount(p.Output.ScriptHash).Contract.IsStandard), asset_id, amount) ?? base.FindUnspentCoins(asset_id, amount, from));
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            TR.Enter();
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out UserWalletAccount account);
                return TR.Exit(account);
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            TR.Enter();
            lock (accounts)
            {
                foreach (UserWalletAccount account in accounts.Values)
                    yield return TR.Exit(account);
            }
        }

        public override IEnumerable<Coin> GetCoins(IEnumerable<UInt160> accounts)
        {
            TR.Enter();
            if (unconfirmed.Count == 0)
                return TR.Exit(WalletIndexer.GetCoins(accounts));
            else
                return TR.Exit(GetCoinsInternal());
            IEnumerable<Coin> GetCoinsInternal()
            {
                HashSet<CoinReference> inputs, claims;
                Coin[] coins_unconfirmed;
                lock (unconfirmed)
                {
                    inputs = new HashSet<CoinReference>(unconfirmed.Values.SelectMany(p => p.Inputs));
                    claims = new HashSet<CoinReference>(unconfirmed.Values.OfType<ClaimTransaction>().SelectMany(p => p.Claims));
                    coins_unconfirmed = unconfirmed.Values.Select(tx => tx.Outputs.Select((o, i) => new Coin
                    {
                        Reference = new CoinReference
                        {
                            PrevHash = tx.Hash,
                            PrevIndex = (ushort)i
                        },
                        Output = o,
                        State = CoinState.Unconfirmed
                    })).SelectMany(p => p).ToArray();
                }
                foreach (Coin coin in WalletIndexer.GetCoins(accounts))
                {
                    if (inputs.Contains(coin.Reference))
                    {
                        if (coin.Output.AssetId.Equals(Blockchain.GoverningToken.Hash))
                            yield return TR.Exit(new Coin
                            {
                                Reference = coin.Reference,
                                Output = coin.Output,
                                State = coin.State | CoinState.Spent
                            });
                        continue;
                    }
                    else if (claims.Contains(coin.Reference))
                    {
                        continue;
                    }
                    yield return TR.Exit(coin);
                }
                HashSet<UInt160> accounts_set = new HashSet<UInt160>(accounts);
                foreach (Coin coin in coins_unconfirmed)
                {
                    if (accounts_set.Contains(coin.Output.ScriptHash))
                        yield return TR.Exit(coin);
                }
            }
        }

        public override IEnumerable<UInt256> GetTransactions()
        {
            TR.Enter();
            foreach (UInt256 hash in WalletIndexer.GetTransactions(accounts.Keys))
                yield return TR.Exit(hash);
            lock (unconfirmed)
            {
                foreach (UInt256 hash in unconfirmed.Keys)
                    yield return TR.Exit(hash);
            }
        }

        private Dictionary<UInt160, UserWalletAccount> LoadAccounts()
        {
            TR.Enter();
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                Dictionary<UInt160, UserWalletAccount> accounts = ctx.Addresses.Select(p => p.ScriptHash).AsEnumerable().Select(p => new UserWalletAccount(new UInt160(p))).ToDictionary(p => p.ScriptHash);
                foreach (Contract db_contract in ctx.Contracts.Include(p => p.Account))
                {
                    VerificationContract contract = db_contract.RawData.AsSerializable<VerificationContract>();
                    UserWalletAccount account = accounts[contract.ScriptHash];
                    account.Contract = contract;
                    account.Key = new KeyPair(DecryptPrivateKey(db_contract.Account.PrivateKeyEncrypted));
                }
                return TR.Exit(accounts);
            }
        }

        private byte[] LoadStoredData(string name)
        {
            TR.Enter();
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                return TR.Exit(ctx.Keys.FirstOrDefault(p => p.Name == name)?.Value);
            }
        }

        public static UserWallet Open(string path, string password)
        {
            TR.Enter();
            return TR.Exit(new UserWallet(path, password.ToAesKey(), false));
        }

        public static UserWallet Open(string path, SecureString password)
        {
            TR.Enter();
            return TR.Exit(new UserWallet(path, password.ToAesKey(), false));
        }

        private void SaveStoredData(string name, byte[] value)
        {
            TR.Enter();
            using (WalletDataContext ctx = new WalletDataContext(path))
            {
                SaveStoredData(ctx, name, value);
                ctx.SaveChanges();
            }
            TR.Exit();
        }

        private static void SaveStoredData(WalletDataContext ctx, string name, byte[] value)
        {
            TR.Enter();
            Key key = ctx.Keys.FirstOrDefault(p => p.Name == name);
            if (key == null)
            {
                ctx.Keys.Add(new Key
                {
                    Name = name,
                    Value = value
                });
            }
            else
            {
                key.Value = value;
            }
            TR.Exit();
        }

        public override bool VerifyPassword(string password)
        {
            TR.Enter();
            return TR.Exit(password.ToAesKey().Sha256().SequenceEqual(LoadStoredData("PasswordHash")));
        }

        private void WalletIndexer_BalanceChanged(object sender, BalanceEventArgs e)
        {
            TR.Enter();
            lock (unconfirmed)
            {
                unconfirmed.Remove(e.Transaction.Hash);
            }
            UInt160[] relatedAccounts;
            lock (accounts)
            {
                relatedAccounts = e.RelatedAccounts.Where(p => accounts.ContainsKey(p)).ToArray();
            }
            if (relatedAccounts.Length > 0)
            {
                BalanceChanged?.Invoke(this, new BalanceEventArgs
                {
                    Transaction = e.Transaction,
                    RelatedAccounts = relatedAccounts,
                    Height = e.Height,
                    Time = e.Time
                });
            }
            TR.Exit();
        }
    }
}
