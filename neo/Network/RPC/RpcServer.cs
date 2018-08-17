using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Neo.Core;
using Neo.IO;
using Neo.IO.Json;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DbgViewTR;

namespace Neo.Network.RPC
{
    public class RpcServer : IDisposable
    {
        protected readonly LocalNode LocalNode;
        private IWebHost host;

        public RpcServer(LocalNode localNode)
        {
            TR.Enter();
            this.LocalNode = localNode;
            TR.Exit();
        }

        private static JObject CreateErrorResponse(JObject id, int code, string message, JObject data = null)
        {
            TR.Enter();
            JObject response = CreateResponse(id);
            response["error"] = new JObject();
            response["error"]["code"] = code;
            response["error"]["message"] = message;
            if (data != null)
                response["error"]["data"] = data;
            return TR.Exit(response);
        }

        private static JObject CreateResponse(JObject id)
        {
            TR.Enter();
            JObject response = new JObject();
            response["jsonrpc"] = "2.0";
            response["id"] = id;
            return TR.Exit(response);
        }

        public void Dispose()
        {
            TR.Enter();
            if (host != null)
            {
                host.Dispose();
                host = null;
            }
            TR.Exit();
        }

        private static JObject GetInvokeResult(byte[] script)
        {
            TR.Enter();
            ApplicationEngine engine = ApplicationEngine.Run(script);
            JObject json = new JObject();
            json["script"] = script.ToHexString();
            json["state"] = engine.State;
            json["gas_consumed"] = engine.GasConsumed.ToString();
            json["stack"] = new JArray(engine.EvaluationStack.Select(p => p.ToParameter().ToJson()));
            return TR.Exit(json);
        }

        protected virtual JObject Process(string method, JArray _params)
        {
            TR.Enter();
            switch (method)
            {
                case "getaccountstate":
                    {
                        UInt160 script_hash = Wallet.ToScriptHash(_params[0].AsString());
                        AccountState account = Blockchain.Default.GetAccountState(script_hash) ?? new AccountState(script_hash);
                        return TR.Exit(account.ToJson());
                    }
                case "getassetstate":
                    {
                        UInt256 asset_id = UInt256.Parse(_params[0].AsString());
                        AssetState asset = Blockchain.Default.GetAssetState(asset_id);
                        TR.Exit();
                        return asset?.ToJson() ?? throw new RpcException(-100, "Unknown asset");
                    }
                case "getbestblockhash":
                    return TR.Exit(Blockchain.Default.CurrentBlockHash.ToString());
                case "getblock":
                    {
                        Block block;
                        if (_params[0] is JNumber)
                        {
                            uint index = (uint)_params[0].AsNumber();
                            block = Blockchain.Default.GetBlock(index);
                        }
                        else
                        {
                            UInt256 hash = UInt256.Parse(_params[0].AsString());
                            block = Blockchain.Default.GetBlock(hash);
                        }
                        if (block == null)
                        {
                            TR.Exit();
                            throw new RpcException(-100, "Unknown block");
                        }
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        if (verbose)
                        {
                            JObject json = block.ToJson();
                            json["confirmations"] = Blockchain.Default.Height - block.Index + 1;
                            UInt256 hash = Blockchain.Default.GetNextBlockHash(block.Hash);
                            if (hash != null)
                                json["nextblockhash"] = hash.ToString();
                            return TR.Exit(json);
                        }
                        else
                        {
                            return TR.Exit(block.ToArray().ToHexString());
                        }
                    }
                case "getblockcount":
                    return TR.Exit(Blockchain.Default.Height + 1);
                case "getblockhash":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height >= 0 && height <= Blockchain.Default.Height)
                        {
                            return TR.Exit(Blockchain.Default.GetBlockHash(height).ToString());
                        }
                        else
                        {
                            TR.Exit();
                            throw new RpcException(-100, "Invalid Height");
                        }
                    }
                case "getblocksysfee":
                    {
                        uint height = (uint)_params[0].AsNumber();
                        if (height >= 0 && height <= Blockchain.Default.Height)
                        {
                            return TR.Exit(Blockchain.Default.GetSysFeeAmount(height).ToString());
                        }
                        else
                        {
                            TR.Exit();
                            throw new RpcException(-100, "Invalid Height");
                        }
                    }
                case "getconnectioncount":
                    return TR.Exit(LocalNode.RemoteNodeCount);
                case "getcontractstate":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractState contract = Blockchain.Default.GetContract(script_hash);
                        TR.Exit();
                        return contract?.ToJson() ?? throw new RpcException(-100, "Unknown contract");
                    }
                case "getrawmempool":
                    return TR.Exit(new JArray(LocalNode.GetMemoryPool().Select(p => (JObject)p.Hash.ToString())));
                case "getrawtransaction":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        bool verbose = _params.Count >= 2 && _params[1].AsBooleanOrDefault(false);
                        int height = -1;
                        Transaction tx = LocalNode.GetTransaction(hash);
                        if (tx == null)
                            tx = Blockchain.Default.GetTransaction(hash, out height);
                        if (tx == null)
                        {
                            TR.Exit();
                            throw new RpcException(-100, "Unknown transaction");
                        }
                        if (verbose)
                        {
                            JObject json = tx.ToJson();
                            if (height >= 0)
                            {
                                Header header = Blockchain.Default.GetHeader((uint)height);
                                json["blockhash"] = header.Hash.ToString();
                                json["confirmations"] = Blockchain.Default.Height - header.Index + 1;
                                json["blocktime"] = header.Timestamp;
                            }
                            return TR.Exit(json);
                        }
                        else
                        {
                            return TR.Exit(tx.ToArray().ToHexString());
                        }
                    }
                case "getstorage":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        byte[] key = _params[1].AsString().HexToBytes();
                        StorageItem item = Blockchain.Default.GetStorageItem(new StorageKey
                        {
                            ScriptHash = script_hash,
                            Key = key
                        }) ?? new StorageItem();
                        return TR.Exit(item.Value?.ToHexString());
                    }
                case "gettxout":
                    {
                        UInt256 hash = UInt256.Parse(_params[0].AsString());
                        ushort index = (ushort)_params[1].AsNumber();
                        return TR.Exit(Blockchain.Default.GetUnspent(hash, index)?.ToJson(index));
                    }
                case "getvalidators":
                    {
                        var validators = Blockchain.Default.GetValidators();
                        return TR.Exit(Blockchain.Default.GetEnrollments().Select(p =>
                        {
                            JObject validator = new JObject();
                            validator["publickey"] = p.PublicKey.ToString();
                            validator["votes"] = p.Votes.ToString();
                            validator["active"] = validators.Contains(p.PublicKey);
                            return validator;
                        }).ToArray());
                    }
                case "invoke":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        ContractParameter[] parameters = ((JArray)_params[1]).Select(p => ContractParameter.FromJson(p)).ToArray();
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            script = sb.EmitAppCall(script_hash, parameters).ToArray();
                        }
                        return TR.Exit(GetInvokeResult(script));
                    }
                case "invokefunction":
                    {
                        UInt160 script_hash = UInt160.Parse(_params[0].AsString());
                        string operation = _params[1].AsString();
                        ContractParameter[] args = _params.Count >= 3 ? ((JArray)_params[2]).Select(p => ContractParameter.FromJson(p)).ToArray() : new ContractParameter[0];
                        byte[] script;
                        using (ScriptBuilder sb = new ScriptBuilder())
                        {
                            script = sb.EmitAppCall(script_hash, operation, args).ToArray();
                        }
                        return TR.Exit(GetInvokeResult(script));
                    }
                case "invokescript":
                    {
                        byte[] script = _params[0].AsString().HexToBytes();
                        return TR.Exit(GetInvokeResult(script));
                    }
                case "sendrawtransaction":
                    {
                        Transaction tx = Transaction.DeserializeFrom(_params[0].AsString().HexToBytes());
                        return TR.Exit(LocalNode.Relay(tx));
                    }
                case "submitblock":
                    {
                        Block block = _params[0].AsString().HexToBytes().AsSerializable<Block>();
                        return TR.Exit(LocalNode.Relay(block));
                    }
                case "validateaddress":
                    {
                        JObject json = new JObject();
                        UInt160 scriptHash;
                        try
                        {
                            scriptHash = Wallet.ToScriptHash(_params[0].AsString());
                        }
                        catch
                        {
                            scriptHash = null;
                        }
                        json["address"] = _params[0];
                        json["isvalid"] = scriptHash != null;
                        return TR.Exit(json);
                    }
                case "getpeers":
                    {
                        JObject json = new JObject();

                        {
                            JArray unconnectedPeers = new JArray();
                            foreach (IPEndPoint peer in LocalNode.GetUnconnectedPeers())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = peer.Address.ToString();
                                peerJson["port"] = peer.Port;
                                unconnectedPeers.Add(peerJson);
                            }
                            json["unconnected"] = unconnectedPeers;
                        }

                        {
                            JArray badPeers = new JArray();
                            foreach (IPEndPoint peer in LocalNode.GetBadPeers())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = peer.Address.ToString();
                                peerJson["port"] = peer.Port;
                                badPeers.Add(peerJson);
                            }
                            json["bad"] = badPeers;
                        }

                        {
                            JArray connectedPeers = new JArray();
                            foreach (RemoteNode node in LocalNode.GetRemoteNodes())
                            {
                                JObject peerJson = new JObject();
                                peerJson["address"] = node.RemoteEndpoint.Address.ToString();
                                peerJson["port"] = node.ListenerEndpoint?.Port ?? 0;
                                connectedPeers.Add(peerJson);
                            }
                            json["connected"] = connectedPeers;
                        }

                        return TR.Exit(json);
                    }
                case "getversion":
                    {
                        JObject json = new JObject();
                        json["port"] = LocalNode.Port;
                        json["nonce"] = LocalNode.Nonce;
                        json["useragent"] = LocalNode.UserAgent;
                        return TR.Exit(json);
                    }
                default:
                    {
                        TR.Exit();
                        throw new RpcException(-32601, "Method not found");
                    }
            }
        }

        private async Task ProcessAsync(HttpContext context)
        {
            TR.Enter();
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            if (context.Request.Method != "GET" && context.Request.Method != "POST")
            {
                TR.Exit();
                return;
            }
            JObject request = null;
            if (context.Request.Method == "GET")
            {
                string jsonrpc = context.Request.Query["jsonrpc"];
                string id = context.Request.Query["id"];
                string method = context.Request.Query["method"];
                string _params = context.Request.Query["params"];
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(method) && !string.IsNullOrEmpty(_params))
                {
                    try
                    {
                        _params = Encoding.UTF8.GetString(Convert.FromBase64String(_params));
                    }
                    catch (FormatException) { }
                    request = new JObject();
                    if (!string.IsNullOrEmpty(jsonrpc))
                        request["jsonrpc"] = jsonrpc;
                    request["id"] = double.Parse(id);
                    request["method"] = method;
                    request["params"] = JObject.Parse(_params);
                }
            }
            else if (context.Request.Method == "POST")
            {
                using (StreamReader reader = new StreamReader(context.Request.Body))
                {
                    try
                    {
                        request = JObject.Parse(reader);
                    }
                    catch (FormatException) { }
                }
            }
            JObject response;
            if (request == null)
            {
                response = CreateErrorResponse(null, -32700, "Parse error");
            }
            else if (request is JArray array)
            {
                if (array.Count == 0)
                {
                    response = CreateErrorResponse(request["id"], -32600, "Invalid Request");
                }
                else
                {
                    response = array.Select(p => ProcessRequest(context, p)).Where(p => p != null).ToArray();
                }
            }
            else
            {
                response = ProcessRequest(context, request);
            }
            if (response == null || (response as JArray)?.Count == 0)
            {
                TR.Exit();
                return;
            }
            context.Response.ContentType = "application/json-rpc";
            await context.Response.WriteAsync(response.ToString(), Encoding.UTF8);
            TR.Exit();
        }

        private JObject ProcessRequest(HttpContext context, JObject request)
        {
            TR.Enter();
            if (!request.ContainsProperty("id"))
            {
                TR.Exit();
                return null;
            }
            if (!request.ContainsProperty("method") || !request.ContainsProperty("params") || !(request["params"] is JArray))
            {
                return TR.Exit(CreateErrorResponse(request["id"], -32600, "Invalid Request"));
            }
            JObject result = null;
            try
            {
                string method = request["method"].AsString();
                JArray _params = (JArray)request["params"];
                foreach (RpcPlugin plugin in RpcPlugin.Instances)
                {
                    result = plugin.OnProcess(context, method, _params);
                    if (result != null) break;
                }
                if (result == null)
                    result = Process(method, _params);
            }
            catch (Exception ex)
            {
#if DEBUG
                return TR.Exit(CreateErrorResponse(request["id"], ex.HResult, ex.Message, ex.StackTrace));
#else
                return CreateErrorResponse(request["id"], ex.HResult, ex.Message);
#endif
            }
            JObject response = CreateResponse(request["id"]);
            response["result"] = result;
            return TR.Exit(response);
        }

        public void Start(int port, string sslCert = null, string password = null)
        {
            TR.Enter();
            host = new WebHostBuilder().UseKestrel(options => options.Listen(IPAddress.Any, port, listenOptions =>
            {
                if (!string.IsNullOrEmpty(sslCert))
                    listenOptions.UseHttps(sslCert, password);
            }))
            .Configure(app =>
            {
                app.UseResponseCompression();
                app.Run(ProcessAsync);
            })
            .ConfigureServices(services =>
            {
                services.AddResponseCompression(options =>
                {
                    // options.EnableForHttps = false;
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json-rpc" });
                });

                services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = CompressionLevel.Fastest;
                });
            })
            .Build();

            host.Start();
            TR.Exit();
        }
    }
}