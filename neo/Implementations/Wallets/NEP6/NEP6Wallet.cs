using Neo.Core;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using UserWallet = Neo.Implementations.Wallets.EntityFramework.UserWallet;
using DbgViewTR;

namespace Neo.Implementations.Wallets.NEP6
{
    public class NEP6Wallet : Wallet, IDisposable
    {
        public override event EventHandler<BalanceEventArgs> BalanceChanged;

        private readonly string path;
        private string password;
        private string name;
        private Version version;
        public readonly ScryptParameters Scrypt;
        private readonly Dictionary<UInt160, NEP6Account> accounts;
        private readonly JObject extra;
        private readonly Dictionary<UInt256, Transaction> unconfirmed = new Dictionary<UInt256, Transaction>();

        public override string Name => name;
        public override Version Version => version;
        public override uint WalletHeight => WalletIndexer.IndexHeight;

        public NEP6Wallet(string path, string name = null)
        {
            TR.Enter();
            this.path = path;
            if (File.Exists(path))
            {
                JObject wallet;
                using (StreamReader reader = new StreamReader(path))
                {
                    wallet = JObject.Parse(reader);
                }
                this.name = wallet["name"]?.AsString();
                this.version = Version.Parse(wallet["version"].AsString());
                this.Scrypt = ScryptParameters.FromJson(wallet["scrypt"]);
                this.accounts = ((JArray)wallet["accounts"]).Select(p => NEP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
                this.extra = wallet["extra"];
                WalletIndexer.RegisterAccounts(accounts.Keys);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this.accounts = new Dictionary<UInt160, NEP6Account>();
                this.extra = JObject.Null;
            }
            WalletIndexer.BalanceChanged += WalletIndexer_BalanceChanged;
            TR.Exit();
        }

        private void AddAccount(NEP6Account account, bool is_import)
        {
            TR.Enter();
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out NEP6Account account_old))
                {
                    account.Label = account_old.Label;
                    account.IsDefault = account_old.IsDefault;
                    account.Lock = account_old.Lock;
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                    else
                    {
                        NEP6Contract contract_old = (NEP6Contract)account_old.Contract;
                        if (contract_old != null)
                        {
                            NEP6Contract contract = (NEP6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                else
                {
                    WalletIndexer.RegisterAccounts(new[] { account.ScriptHash }, is_import ? 0 : Blockchain.Default?.Height ?? 0);
                }
                accounts[account.ScriptHash] = account;
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

        public override bool Contains(UInt160 scriptHash)
        {
            TR.Enter();
            lock (accounts)
            {
                return TR.Exit(accounts.ContainsKey(scriptHash));
            }
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            TR.Enter();
            KeyPair key = new KeyPair(privateKey);
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, false);
            return TR.Exit(account);
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            TR.Enter();
            NEP6Contract nep6contract = contract as NEP6Contract;
            if (nep6contract == null)
            {
                nep6contract = new NEP6Contract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList,
                    ParameterNames = contract.ParameterList.Select((p, i) => $"parameter{i}").ToArray(),
                    Deployed = false
                };
            }
            NEP6Account account;
            if (key == null)
                account = new NEP6Account(this, nep6contract.ScriptHash);
            else
                account = new NEP6Account(this, nep6contract.ScriptHash, key, password);
            account.Contract = nep6contract;
            AddAccount(account, false);
            return TR.Exit(account);
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            TR.Enter();
            NEP6Account account = new NEP6Account(this, scriptHash);
            AddAccount(account, true);
            return TR.Exit(account);
        }

        public KeyPair DecryptKey(string nep2key)
        {
            TR.Enter();
            return TR.Exit(new KeyPair(GetPrivateKeyFromNEP2(nep2key, password, Scrypt.N, Scrypt.R, Scrypt.P)));
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            TR.Enter();
            bool removed;
            lock (accounts)
            {
                removed = accounts.Remove(scriptHash);
            }
            if (removed)
            {
                WalletIndexer.UnregisterAccounts(new[] { scriptHash });
            }
            return TR.Exit(removed);
        }

        public void Dispose()
        {
            TR.Enter();
            WalletIndexer.BalanceChanged -= WalletIndexer_BalanceChanged;
            TR.Exit();
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
                accounts.TryGetValue(scriptHash, out NEP6Account account);
                return TR.Exit(account);
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            TR.Enter();
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values)
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

        public override WalletAccount Import(X509Certificate2 cert)
        {
            TR.Enter();
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D);
            }
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return TR.Exit(account);
        }

        public override WalletAccount Import(string wif)
        {
            TR.Enter();
            KeyPair key = new KeyPair(GetPrivateKeyFromWIF(wif));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return TR.Exit(account);
        }

        public override WalletAccount Import(string nep2, string passphrase)
        {
            TR.Enter();
            KeyPair key = new KeyPair(GetPrivateKeyFromNEP2(nep2, passphrase));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account;
            if (Scrypt.N == 16384 && Scrypt.R == 8 && Scrypt.P == 8)
                account = new NEP6Account(this, contract.ScriptHash, nep2);
            else
                account = new NEP6Account(this, contract.ScriptHash, key, passphrase);
            account.Contract = contract;
            AddAccount(account, true);
            return TR.Exit(account);
        }

        internal void Lock()
        {
            TR.Enter();
            password = null;
            TR.Exit();
        }

        public static NEP6Wallet Migrate(string path, string db3path, string password)
        {
            TR.Enter();
            using (UserWallet wallet_old = UserWallet.Open(db3path, password))
            {
                NEP6Wallet wallet_new = new NEP6Wallet(path, wallet_old.Name);
                using (wallet_new.Unlock(password))
                {
                    foreach (WalletAccount account in wallet_old.GetAccounts())
                    {
                        wallet_new.CreateAccount(account.Contract, account.GetKey());
                    }
                }
                return TR.Exit(wallet_new);
            }
        }

        public void Save()
        {
            TR.Enter();
            JObject wallet = new JObject();
            wallet["name"] = name;
            wallet["version"] = version.ToString();
            wallet["scrypt"] = Scrypt.ToJson();
            wallet["accounts"] = new JArray(accounts.Values.Select(p => p.ToJson()));
            wallet["extra"] = extra;
            File.WriteAllText(path, wallet.ToString());
            TR.Exit();
        }

        public IDisposable Unlock(string password)
        {
            TR.Enter();
            if (!VerifyPassword(password))
            {
                TR.Exit();
                throw new CryptographicException();
            }
            this.password = password;
            return TR.Exit(new WalletLocker(this));
        }

        public override bool VerifyPassword(string password)
        {
            TR.Enter();
            lock (accounts)
            {
                NEP6Account account = accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = accounts.Values.FirstOrDefault(p => p.HasKey);
                }
                if (account == null) return TR.Exit(true);
                if (account.Decrypted)
                {
                    return TR.Exit(account.VerifyPassword(password));
                }
                else
                {
                    try
                    {
                        account.GetKey(password);
                        return TR.Exit(true);
                    }
                    catch (FormatException)
                    {
                        return TR.Exit(false);
                    }
                }
            }
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
