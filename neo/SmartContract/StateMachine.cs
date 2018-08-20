﻿using Neo.Core;
using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DbgViewTR;

namespace Neo.SmartContract
{
    public class StateMachine : StateReader
    {
        private readonly Block persisting_block;
        private readonly DataCache<UInt160, AccountState> accounts;
        private readonly DataCache<UInt256, AssetState> assets;
        private readonly DataCache<UInt160, ContractState> contracts;
        private readonly DataCache<StorageKey, StorageItem> storages;

        private Dictionary<UInt160, UInt160> contracts_created = new Dictionary<UInt160, UInt160>();

        protected override DataCache<UInt160, AccountState> Accounts => accounts;
        protected override DataCache<UInt256, AssetState> Assets => assets;
        protected override DataCache<UInt160, ContractState> Contracts => contracts;
        protected override DataCache<StorageKey, StorageItem> Storages => storages;

        public StateMachine(Block persisting_block, DataCache<UInt160, AccountState> accounts, DataCache<UInt256, AssetState> assets, DataCache<UInt160, ContractState> contracts, DataCache<StorageKey, StorageItem> storages)
        {
            TR.Enter();
            this.persisting_block = persisting_block;
            this.accounts = accounts.CreateSnapshot();
            this.assets = assets.CreateSnapshot();
            this.contracts = contracts.CreateSnapshot();
            this.storages = storages.CreateSnapshot();

            //Standard Library
            Register("System.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("System.Contract.Destroy", Contract_Destroy);
            Register("System.Storage.Put", Storage_Put);
            Register("System.Storage.Delete", Storage_Delete);

            //Neo Specified
            Register("Neo.Asset.Create", Asset_Create);
            Register("Neo.Asset.Renew", Asset_Renew);
            Register("Neo.Contract.Create", Contract_Create);
            Register("Neo.Contract.Migrate", Contract_Migrate);
            TR.Exit();

            #region Old APIs
            Register("AntShares.Asset.Create", Asset_Create);
            Register("AntShares.Asset.Renew", Asset_Renew);
            Register("AntShares.Contract.Create", Contract_Create);
            Register("AntShares.Contract.Migrate", Contract_Migrate);
            Register("Neo.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("AntShares.Contract.GetStorageContext", Contract_GetStorageContext);
            Register("Neo.Contract.Destroy", Contract_Destroy);
            Register("AntShares.Contract.Destroy", Contract_Destroy);
            Register("Neo.Storage.Put", Storage_Put);
            Register("AntShares.Storage.Put", Storage_Put);
            Register("Neo.Storage.Delete", Storage_Delete);
            Register("AntShares.Storage.Delete", Storage_Delete);
            #endregion
        }

        public void Commit()
        {
            TR.Enter();
            accounts.Commit();
            assets.Commit();
            contracts.Commit();
            storages.Commit();
            TR.Exit();
        }

        protected override bool Runtime_GetTime(ExecutionEngine engine)
        {
            TR.Enter();
            engine.EvaluationStack.Push(persisting_block.Timestamp);
            return TR.Exit(true);
        }

        private bool Asset_Create(ExecutionEngine engine)
        {
            TR.Enter();
            InvocationTransaction tx = (InvocationTransaction)engine.ScriptContainer;
            AssetType asset_type = (AssetType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (!Enum.IsDefined(typeof(AssetType), asset_type) || asset_type == AssetType.CreditFlag || asset_type == AssetType.DutyFlag || asset_type == AssetType.GoverningToken || asset_type == AssetType.UtilityToken)
                return TR.Exit(false);
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 1024)
                return TR.Exit(false);
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            Fixed8 amount = new Fixed8((long)engine.EvaluationStack.Pop().GetBigInteger());
            if (amount == Fixed8.Zero || amount < -Fixed8.Satoshi) return TR.Exit(false);
            if (asset_type == AssetType.Invoice && amount != -Fixed8.Satoshi)
                return TR.Exit(false);
            byte precision = (byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (precision > 8) return TR.Exit(false);
            if (asset_type == AssetType.Share && precision != 0) return TR.Exit(false);
            if (amount != -Fixed8.Satoshi && amount.GetData() % (long)Math.Pow(10, 8 - precision) != 0)
                return TR.Exit(false);
            ECPoint owner = ECPoint.DecodePoint(engine.EvaluationStack.Pop().GetByteArray(), ECCurve.Secp256r1);
            if (owner.IsInfinity) return TR.Exit(false);
            if (!CheckWitness(engine, owner))
                return TR.Exit(false);
            UInt160 admin = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 issuer = new UInt160(engine.EvaluationStack.Pop().GetByteArray());
            AssetState asset = assets.GetOrAdd(tx.Hash, () => new AssetState
            {
                AssetId = tx.Hash,
                AssetType = asset_type,
                Name = name,
                Amount = amount,
                Available = Fixed8.Zero,
                Precision = precision,
                Fee = Fixed8.Zero,
                FeeAddress = new UInt160(),
                Owner = owner,
                Admin = admin,
                Issuer = issuer,
                Expiration = Blockchain.Default.Height + 1 + 2000000,
                IsFrozen = false
            });
            engine.EvaluationStack.Push(StackItem.FromInterface(asset));
            return TR.Exit(true);
        }

        private bool Asset_Renew(ExecutionEngine engine)
        {
            TR.Enter();
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                AssetState asset = _interface.GetInterface<AssetState>();
                if (asset == null) return TR.Exit(false);
                byte years = (byte)engine.EvaluationStack.Pop().GetBigInteger();
                asset = assets.GetAndChange(asset.AssetId);
                if (asset.Expiration < Blockchain.Default.Height + 1)
                    asset.Expiration = Blockchain.Default.Height + 1;
                try
                {
                    asset.Expiration = checked(asset.Expiration + years * 2000000u);
                }
                catch (OverflowException)
                {
                    asset.Expiration = uint.MaxValue;
                }
                engine.EvaluationStack.Push(asset.Expiration);
                return TR.Exit(true);
            }
            return TR.Exit(false);
        }

        private bool Contract_Create(ExecutionEngine engine)
        {
            TR.Enter();
            byte[] script = engine.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return TR.Exit(false);
            ContractParameterType[] parameter_list = engine.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return TR.Exit(false);
            ContractParameterType return_type = (ContractParameterType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string version = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string author = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string email = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 65536) return TR.Exit(false);
            string description = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                contracts.Add(hash, contract);
                contracts_created.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return TR.Exit(true);
        }

        private bool Contract_Migrate(ExecutionEngine engine)
        {
            TR.Enter();
            byte[] script = engine.EvaluationStack.Pop().GetByteArray();
            if (script.Length > 1024 * 1024) return TR.Exit(false);
            ContractParameterType[] parameter_list = engine.EvaluationStack.Pop().GetByteArray().Select(p => (ContractParameterType)p).ToArray();
            if (parameter_list.Length > 252) return TR.Exit(false);
            ContractParameterType return_type = (ContractParameterType)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            ContractPropertyState contract_properties = (ContractPropertyState)(byte)engine.EvaluationStack.Pop().GetBigInteger();
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string name = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string version = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string author = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 252) return TR.Exit(false);
            string email = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            if (engine.EvaluationStack.Peek().GetByteArray().Length > 65536) return TR.Exit(false);
            string description = Encoding.UTF8.GetString(engine.EvaluationStack.Pop().GetByteArray());
            UInt160 hash = script.ToScriptHash();
            ContractState contract = contracts.TryGet(hash);
            if (contract == null)
            {
                contract = new ContractState
                {
                    Script = script,
                    ParameterList = parameter_list,
                    ReturnType = return_type,
                    ContractProperties = contract_properties,
                    Name = name,
                    CodeVersion = version,
                    Author = author,
                    Email = email,
                    Description = description
                };
                contracts.Add(hash, contract);
                contracts_created.Add(hash, new UInt160(engine.CurrentContext.ScriptHash));
                if (contract.HasStorage)
                {
                    foreach (var pair in storages.Find(engine.CurrentContext.ScriptHash).ToArray())
                    {
                        storages.Add(new StorageKey
                        {
                            ScriptHash = hash,
                            Key = pair.Key.Key
                        }, new StorageItem
                        {
                            Value = pair.Value.Value
                        });
                    }
                }
            }
            engine.EvaluationStack.Push(StackItem.FromInterface(contract));
            return TR.Exit(Contract_Destroy(engine));
        }

        private bool Contract_GetStorageContext(ExecutionEngine engine)
        {
            TR.Enter();
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                ContractState contract = _interface.GetInterface<ContractState>();
                if (!contracts_created.TryGetValue(contract.ScriptHash, out UInt160 created)) return TR.Exit(false);
                if (!created.Equals(new UInt160(engine.CurrentContext.ScriptHash))) return TR.Exit(false);
                engine.EvaluationStack.Push(StackItem.FromInterface(new StorageContext
                {
                    ScriptHash = contract.ScriptHash,
                    IsReadOnly = false
                }));
                return TR.Exit(true);
            }
            return TR.Exit(false);
        }

        private bool Contract_Destroy(ExecutionEngine engine)
        {
            TR.Enter();
            UInt160 hash = new UInt160(engine.CurrentContext.ScriptHash);
            ContractState contract = contracts.TryGet(hash);
            if (contract == null) return TR.Exit(true);
            contracts.Delete(hash);
            if (contract.HasStorage)
                foreach (var pair in storages.Find(hash.ToArray()))
                    storages.Delete(pair.Key);
            return TR.Exit(true);
        }

        private bool Storage_Put(ExecutionEngine engine)
        {
            TR.Enter();
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (context.IsReadOnly) return TR.Exit(false);
                if (!CheckStorageContext(context)) return TR.Exit(false);
                byte[] key = engine.EvaluationStack.Pop().GetByteArray();
                if (key.Length > 1024) return TR.Exit(false);
                byte[] value = engine.EvaluationStack.Pop().GetByteArray();
                storages.GetAndChange(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                }, () => new StorageItem()).Value = value;
                return TR.Exit(true);
            }
            return TR.Exit(false);
        }

        private bool Storage_Delete(ExecutionEngine engine)
        {
            TR.Enter();
            if (engine.EvaluationStack.Pop() is InteropInterface _interface)
            {
                StorageContext context = _interface.GetInterface<StorageContext>();
                if (context.IsReadOnly) return TR.Exit(false);
                if (!CheckStorageContext(context)) return TR.Exit(false);
                byte[] key = engine.EvaluationStack.Pop().GetByteArray();
                storages.Delete(new StorageKey
                {
                    ScriptHash = context.ScriptHash,
                    Key = key
                });
                return TR.Exit(true);
            }
            return TR.Exit(false);
        }
    }
}
