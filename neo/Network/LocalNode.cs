using NoDbgViewTR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Neo.Core;
using Neo.IO;
using Neo.IO.Caching;
using Neo.Network.Payloads;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    public class LocalNode : IDisposable
    {
        public static event EventHandler<InventoryReceivingEventArgs> InventoryReceiving;
        public static event EventHandler<IInventory> InventoryReceived;

        public const uint ProtocolVersion = 0;
        private const int ConnectedMax = 10;
        private const int DesiredAvailablePeers = (int)(ConnectedMax * 1.5);
        private const int UnconnectedMax = 1000;
        public const int MemoryPoolSize = 50000;
        internal static readonly TimeSpan HashesExpiration = TimeSpan.FromSeconds(30);
        private DateTime LastBlockReceived = DateTime.UtcNow;

        private static readonly Dictionary<UInt256, Transaction> mem_pool = new Dictionary<UInt256, Transaction>();
        private readonly HashSet<Transaction> temp_pool = new HashSet<Transaction>();
        internal static readonly Dictionary<UInt256, DateTime> KnownHashes = new Dictionary<UInt256, DateTime>();
        internal readonly RelayCache RelayCache = new RelayCache(100);

        private static readonly HashSet<IPEndPoint> unconnectedPeers = new HashSet<IPEndPoint>();
        private static readonly HashSet<IPEndPoint> badPeers = new HashSet<IPEndPoint>();
        internal readonly List<RemoteNode> connectedPeers = new List<RemoteNode>();

        internal static readonly HashSet<IPAddress> LocalAddresses = new HashSet<IPAddress>();
        internal ushort Port;
        internal readonly uint Nonce;
        private TcpListener listener;
        private IWebHost ws_host;
        private Thread connectThread;
        private Thread poolThread;
        private readonly AutoResetEvent new_tx_event = new AutoResetEvent(false);
        private int started = 0;
        private int disposed = 0;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public bool GlobalMissionsEnabled { get; set; } = true;
        public int RemoteNodeCount => connectedPeers.Count;
        public bool ServiceEnabled { get; set; } = true;
        public bool UpnpEnabled { get; set; } = false;
        public string UserAgent { get; set; }

        static LocalNode()
        {
            TR.Enter();
            LocalAddresses.UnionWith(NetworkInterface.GetAllNetworkInterfaces().SelectMany(p => p.GetIPProperties().UnicastAddresses).Select(p => p.Address.MapToIPv6()));
            TR.Exit();
        }

        public LocalNode()
        {
            TR.Enter();
            Random rand = new Random();
            this.Nonce = (uint)rand.Next();
            this.connectThread = new Thread(ConnectToPeersLoop)
            {
                IsBackground = true,
                Name = "LocalNode.ConnectToPeersLoop"
            };
            if (Blockchain.Default != null)
            {
                this.poolThread = new Thread(AddTransactionLoop)
                {
                    IsBackground = true,
                    Name = "LocalNode.AddTransactionLoop"
                };
            }
            this.UserAgent = string.Format("/NEO:{0}/", Assembly.GetExecutingAssembly().GetVersion());
            Blockchain.PersistCompleted += Blockchain_PersistCompleted;
            TR.Exit();
        }

        private async void AcceptPeers()
        {
#if !NET47
            //There is a bug in .NET Core 2.0 that blocks async method which returns void.
            await Task.Yield();
#endif
            TR.Enter();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                Socket socket;
                IndentContext ic = TR.SaveContextAndShuffle();
                try
                {
                    socket = await listener.AcceptSocketAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    continue;
                }
                finally
                {
                    TR.RestoreContext(ic);
                }
                TcpRemoteNode remoteNode = new TcpRemoteNode(this, socket);
                OnConnected(remoteNode);
            }
            TR.Exit();
        }

        private static bool AddTransaction(Transaction tx)
        {
            TR.Enter();
            if (Blockchain.Default == null) return TR.Exit(false);
            lock (Blockchain.Default.PersistLock)
            {
                lock (mem_pool)
                {
                    if (mem_pool.ContainsKey(tx.Hash)) return TR.Exit(false);
                    if (Blockchain.Default.ContainsTransaction(tx.Hash)) return TR.Exit(false);
                    if (!tx.Verify(mem_pool.Values)) return TR.Exit(false);
                    mem_pool.Add(tx.Hash, tx);
                    CheckMemPool();
                }
            }
            return TR.Exit(true);
        }

        private void AddTransactionLoop()
        {
            TR.Enter();
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                new_tx_event.WaitOne();
                Transaction[] transactions;
                lock (temp_pool)
                {
                    if (temp_pool.Count == 0) continue;
                    transactions = temp_pool.ToArray();
                    temp_pool.Clear();
                }
                ConcurrentBag<Transaction> verified = new ConcurrentBag<Transaction>();
                lock (Blockchain.Default.PersistLock)
                {
                    lock (mem_pool)
                    {
                        transactions = transactions.Where(p => !mem_pool.ContainsKey(p.Hash) && !Blockchain.Default.ContainsTransaction(p.Hash)).ToArray();

                        if (transactions.Length == 0)
                            continue;

                        Transaction[] tmpool = mem_pool.Values.Concat(transactions).ToArray();

                        ParallelOptions po = new ParallelOptions();
                        po.CancellationToken = Blockchain.Default.VerificationCancellationToken.Token;
                        po.MaxDegreeOfParallelism = System.Environment.ProcessorCount;

                        try
                        {
                            Parallel.ForEach(transactions.AsParallel(), po, tx =>
                            {
                                if (tx.Verify(tmpool))
                                    verified.Add(tx);
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            lock (temp_pool)
                            {
                                foreach (Transaction tx in transactions)
                                    temp_pool.Add(tx);
                            }

                            continue;
                        }

                        if (verified.Count == 0) continue;

                        foreach (Transaction tx in verified)
                            mem_pool.Add(tx.Hash, tx);

                        CheckMemPool();
                    }
                }
                RelayDirectly(verified);
                if (InventoryReceived != null)
                    foreach (Transaction tx in verified)
                        InventoryReceived(this, tx);
            }
            TR.Exit();
        }

        public static void AllowHashes(IEnumerable<UInt256> hashes)
        {
            TR.Enter();
            lock (KnownHashes)
            {
                foreach (UInt256 hash in hashes)
                    KnownHashes.Remove(hash);
            }
            TR.Exit();
        }

        private void Blockchain_PersistCompleted(object sender, Block block)
        {
            TR.Enter();
            Transaction[] remain;
            var millisSinceLastBlock = TimeSpan.FromTicks(DateTimeOffset.UtcNow.Ticks)
                .Subtract(TimeSpan.FromTicks(LastBlockReceived.Ticks)).TotalMilliseconds;

            lock (mem_pool)
            {
                // Remove the transactions that made it into the block
                foreach (Transaction tx in block.Transactions)
                    mem_pool.Remove(tx.Hash);
                if (mem_pool.Count == 0) return;

                remain = mem_pool.Values.ToArray();
                mem_pool.Clear();
                
                if (millisSinceLastBlock > 10000)
                {
                    ConcurrentBag<Transaction> verified = new ConcurrentBag<Transaction>();
                    // Reverify the remaining transactions in the mem_pool
                    remain.AsParallel().ForAll(tx =>
                    {
                        if (tx.Verify(remain))
                            verified.Add(tx);
                    });
                
                    // Note, when running 
                    foreach (Transaction tx in verified)
                        mem_pool.Add(tx.Hash, tx);                    
                }
            }
            LastBlockReceived = DateTime.UtcNow;
            
            lock (temp_pool)
            {
                if (millisSinceLastBlock > 10000)
                {
                    if (temp_pool.Count > 0)
                        new_tx_event.Set();
                }
                else
                {
                    temp_pool.UnionWith(remain);
                    new_tx_event.Set();
                }
            }
            TR.Exit();
        }

        private static bool CheckKnownHashes(UInt256 hash)
        {
            TR.Enter();
            DateTime now = DateTime.UtcNow;
            lock (KnownHashes)
            {
                if (KnownHashes.TryGetValue(hash, out DateTime time))
                {
                    if (now - time <= HashesExpiration)
                        return TR.Exit(false);
                }
                KnownHashes[hash] = now;
                if (KnownHashes.Count > 1000000)
                {
                    UInt256[] expired = KnownHashes.Where(p => now - p.Value > HashesExpiration).Select(p => p.Key).ToArray();
                    foreach (UInt256 key in expired)
                        KnownHashes.Remove(key);
                }
                return TR.Exit(true);
            }
        }

        private static void CheckMemPool()
        {
            TR.Enter();
            if (mem_pool.Count <= MemoryPoolSize) { TR.Exit(); return; }

             UInt256[] hashes = mem_pool.Values.AsParallel()
                .OrderBy(p => p.NetworkFee / p.Size)
                .ThenBy(p => new BigInteger(p.Hash.ToArray()))
                .Take(mem_pool.Count - MemoryPoolSize)
                .Select(p => p.Hash)
                .ToArray();

            foreach (UInt256 hash in hashes)
                mem_pool.Remove(hash);
            TR.Exit();
        }

        public async Task<IPEndPoint> GetIPEndpointFromHostPortAsync(string hostNameOrAddress, int port)
        {
            TR.Enter();
            if (IPAddress.TryParse(hostNameOrAddress, out IPAddress ipAddress))
            {
                ipAddress = ipAddress.MapToIPv6();
            }
            else
            {
                IPHostEntry entry;
                try
                {
                    IndentContext ic = TR.SaveContextAndShuffle();
                    entry = await Dns.GetHostEntryAsync(hostNameOrAddress);
                    TR.RestoreContext(ic);
                }
                catch (SocketException)
                {
                    return TR.Exit((IPEndPoint) null);
                }
                ipAddress = entry.AddressList.FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo)?.MapToIPv6();
                if (ipAddress == null) return TR.Exit((IPEndPoint)null);
            }

            return TR.Exit(new IPEndPoint(ipAddress, port));
        }

        public async Task ConnectToPeerAsync(string hostNameOrAddress, int port)
        {
            TR.Enter();
            IPEndPoint ipEndpoint = await GetIPEndpointFromHostPortAsync(hostNameOrAddress, port);

            if (ipEndpoint == null) { TR.Exit(); return; }
            TR.Log();
            await ConnectToPeerAsync(ipEndpoint);
            TR.Exit();
        }

        public async Task ConnectToPeerAsync(IPEndPoint remoteEndpoint)
        {
            TR.Enter();
            if (remoteEndpoint.Port == Port && LocalAddresses.Contains(remoteEndpoint.Address.MapToIPv6())) { TR.Exit(); return; }
            lock (unconnectedPeers)
            {
                unconnectedPeers.Remove(remoteEndpoint);
            }
            lock (connectedPeers)
            {
                if (connectedPeers.Any(p => remoteEndpoint.Equals(p.ListenerEndpoint)))
                {
                    TR.Exit();
                    return;
                }
            }
            TR.Log();
            TcpRemoteNode remoteNode = new TcpRemoteNode(this, remoteEndpoint);
            TR.Log();
            IndentContext ic = TR.SaveContextAndShuffle();
            bool connected = await remoteNode.ConnectAsync();
            TR.RestoreContext(ic);
            if (connected)
            {
                TR.Log();
                OnConnected(remoteNode);
            }
            TR.Exit();
        }

        private IEnumerable<IPEndPoint> GetIPEndPointsFromSeedList(int seedsToTake)
        {
            TR.Log();
            if (seedsToTake > 0)
            {
                Random rand = new Random();
                foreach (string hostAndPort in Settings.Default.SeedList.OrderBy(p => rand.Next()))
                {
                    if (seedsToTake == 0) break;
                    string[] p = hostAndPort.Split(':');
                    IPEndPoint seed;
                    IndentContext ic = TR.SaveContextAndShuffle();
                    try
                    {
                        seed = GetIPEndpointFromHostPortAsync(p[0], int.Parse(p[1])).Result;
                        TR.RestoreContext(ic);
                    }
                    catch (AggregateException)
                    {
                        TR.RestoreContext(ic);
                        continue;
                    }
                    if (seed == null) continue;
                    seedsToTake--;
                    yield return TR.Log(seed);
                }
            }
            TR.Log();
        }

        private void ConnectToPeersLoop()
        {
            TR.Enter();
            Dictionary<Task, IPAddress> tasksDict = new Dictionary<Task, IPAddress>();
            DateTime lastSufficientPeersTimestamp = DateTime.UtcNow;
            Dictionary<IPAddress, Task> currentlyConnectingIPs = new Dictionary<IPAddress, Task>();

            void connectToPeers(IEnumerable<IPEndPoint> ipEndPoints)
            {
                TR.Enter();
                foreach (var ipEndPoint in ipEndPoints)
                {
                    // Protect from the case same IP is in the endpoint array twice
                    if (currentlyConnectingIPs.ContainsKey(ipEndPoint.Address))
                        continue;

                    TR.Log(ipEndPoint.ToString());
                    IndentContext iu = TR.SaveContextAndShuffle();
                    var connectTask = ConnectToPeerAsync(ipEndPoint);
                    TR.RestoreContext(iu);

                    // Completed tasks that run synchronously may use a non-unique cached task object.
                    if (connectTask.IsCompleted)
                        continue;

                    tasksDict.Add(connectTask, ipEndPoint.Address);
                    currentlyConnectingIPs.Add(ipEndPoint.Address, connectTask);
                }
                TR.Exit();
            }

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                int connectedCount = connectedPeers.Count;
                int unconnectedCount = unconnectedPeers.Count;
                if (connectedCount < ConnectedMax)
                {
                    if (unconnectedCount > 0)
                    {
                        IPEndPoint[] endpoints;
                        lock (unconnectedPeers)
                        {
                            endpoints = unconnectedPeers.Where(x => !currentlyConnectingIPs.ContainsKey(x.Address))
                                .Take(ConnectedMax - connectedCount).ToArray();
                        }

                        connectToPeers(endpoints);
                    }

                    if (connectedCount > 0)
                    {
                        if (unconnectedCount + connectedCount < DesiredAvailablePeers)
                        {
                            lock (connectedPeers)
                            {
                                foreach (RemoteNode node in connectedPeers)
                                    node.RequestPeers();
                            }

                            if (lastSufficientPeersTimestamp < DateTime.UtcNow.AddSeconds(-180))
                            {
                                IEnumerable<IPEndPoint> endpoints = GetIPEndPointsFromSeedList(2);
                                connectToPeers(endpoints);
                                lastSufficientPeersTimestamp = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            lastSufficientPeersTimestamp = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        IEnumerable<IPEndPoint> endpoints = GetIPEndPointsFromSeedList(5);
                        connectToPeers(endpoints);
                        lastSufficientPeersTimestamp = DateTime.UtcNow;
                    }
                }

                try
                {
                    var tasksArray = tasksDict.Keys.ToArray();
                    if (tasksArray.Length > 0)
                    {
                        Task.WaitAny(tasksArray, 5000, cancellationTokenSource.Token);

                        foreach (var task in tasksArray)
                        {
                            if (!task.IsCompleted) continue;
                            if (tasksDict.TryGetValue(task, out IPAddress ip))
                                currentlyConnectingIPs.Remove(ip);
                            // Clean-up task no longer running.
                            tasksDict.Remove(task);
                            task.Dispose();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                for (int i = 0; i < 50 && !cancellationTokenSource.IsCancellationRequested; i++)
                {
                    Thread.Sleep(100);
                }
            }
            TR.Log("ConnectToPeersLoop() exit!!!");
            TR.Exit();
        }

        public static bool ContainsTransaction(UInt256 hash)
        {
            TR.Enter();
            lock (mem_pool)
            {
                return TR.Exit(mem_pool.ContainsKey(hash));
            }
        }

        public void Dispose()
        {
            TR.Enter();
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                cancellationTokenSource.Cancel();
                if (started > 0)
                {
                    // Ensure any outstanding calls to Blockchain_PersistCompleted are not in progress
                    lock (Blockchain.Default.PersistLock)
                    {
                        Blockchain.PersistCompleted -= Blockchain_PersistCompleted;
                    }

                    if (listener != null) listener.Stop();
                    if (!connectThread.ThreadState.HasFlag(ThreadState.Unstarted)) connectThread.Join();
                    lock (unconnectedPeers)
                    {
                        if (unconnectedPeers.Count < UnconnectedMax)
                        {
                            lock (connectedPeers)
                            {
                                unconnectedPeers.UnionWith(connectedPeers.Select(p => p.ListenerEndpoint).Where(p => p != null).Take(UnconnectedMax - unconnectedPeers.Count));
                            }
                        }
                    }
                    RemoteNode[] nodes;
                    lock (connectedPeers)
                    {
                        nodes = connectedPeers.ToArray();
                    }
                    Task.WaitAll(nodes.Select(p => Task.Run(() => p.Disconnect(false))).ToArray());

                    new_tx_event.Set();
                    if (poolThread?.ThreadState.HasFlag(ThreadState.Unstarted) == false)
                        poolThread.Join();

                    new_tx_event.Dispose();
                }
            }
            TR.Exit();
        }

        public static Transaction[] GetMemoryPool()
        {
            TR.Enter();
            lock (mem_pool)
            {
                return TR.Exit(mem_pool.Values.ToArray());
            }
        }

        public RemoteNode[] GetRemoteNodes()
        {
            TR.Enter();
            lock (connectedPeers)
            {
                return TR.Exit(connectedPeers.ToArray());
            }
        }

        public static Transaction GetTransaction(UInt256 hash)
        {
            TR.Enter();
            lock (mem_pool)
            {
                if (!mem_pool.TryGetValue(hash, out Transaction tx))
                    return TR.Exit((Transaction)null);
                return TR.Exit(tx);
            }
        }

        internal void RequestGetBlocks()
        {
            TR.Enter();
            RemoteNode[] nodes = GetRemoteNodes();

            GetBlocksPayload payload = GetBlocksPayload.Create(Blockchain.Default.CurrentBlockHash);

            foreach (RemoteNode node in nodes)
                node.EnqueueMessage("getblocks", payload);
            TR.Exit();
        }

        private static bool IsIntranetAddress(IPAddress address)
        {
            TR.Enter();
            byte[] data = address.MapToIPv4().GetAddressBytes();
            Array.Reverse(data);
            uint value = data.ToUInt32(0);
            return TR.Exit((value & 0xff000000) == 0x0a000000 || (value & 0xff000000) == 0x7f000000 || (value & 0xfff00000) == 0xac100000 || (value & 0xffff0000) == 0xc0a80000 || (value & 0xffff0000) == 0xa9fe0000);
        }

        public static void LoadState(Stream stream)
        {
            TR.Enter();
            lock (unconnectedPeers)
            {
                unconnectedPeers.Clear();
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        IPAddress address = new IPAddress(reader.ReadBytes(4));
                        int port = reader.ReadUInt16();
                        TR.Log("{0}:{1}", address, port);
                        unconnectedPeers.Add(new IPEndPoint(address.MapToIPv6(), port));
                    }
                }
            }
            TR.Exit();
        }

        private void OnConnected(RemoteNode remoteNode)
        {
            TR.Enter();
            lock (connectedPeers)
            {
                connectedPeers.Add(remoteNode);
            }
            remoteNode.Disconnected += RemoteNode_Disconnected;
            remoteNode.InventoryReceived += RemoteNode_InventoryReceived;
            remoteNode.PeersReceived += RemoteNode_PeersReceived;
            remoteNode.StartProtocol();
            TR.Exit();
        }

        private async Task ProcessWebSocketAsync(HttpContext context)
        {
            TR.Enter();
            if (!context.WebSockets.IsWebSocketRequest)
            {
                TR.Exit();
                return;
            }
            IndentContext ic = TR.SaveContextAndShuffle();
            WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();
            TR.RestoreContext(ic);
            WebSocketRemoteNode remoteNode = new WebSocketRemoteNode(this, ws, new IPEndPoint(context.Connection.RemoteIpAddress, context.Connection.RemotePort));
            OnConnected(remoteNode);
            TR.Exit();
        }

        public bool Relay(IInventory inventory)
        {
            TR.Enter();
            if (inventory is MinerTransaction) return TR.Exit(false);
            if (!CheckKnownHashes(inventory.Hash)) return TR.Exit(false);
            InventoryReceivingEventArgs args = new InventoryReceivingEventArgs(inventory);
            InventoryReceiving?.Invoke(this, args);
            if (args.Cancel) return TR.Exit(false);
            if (inventory is Block block)
            {
                if (Blockchain.Default == null) return TR.Exit(false);
                if (Blockchain.Default.ContainsBlock(block.Hash)) return TR.Exit(false);
                if (!Blockchain.Default.AddBlock(block)) return TR.Exit(false);
            }
            else if (inventory is Transaction)
            {
                if (!AddTransaction((Transaction)inventory)) return TR.Exit(false);
            }
            else //if (inventory is Consensus)
            {
                if (!inventory.Verify()) return TR.Exit(false);
            }
            bool relayed = RelayDirectly(inventory);
            InventoryReceived?.Invoke(this, inventory);
            return TR.Exit(relayed);
        }

        public bool RelayDirectly(IInventory inventory)
        {
            TR.Enter();
            bool relayed = false;
            lock (connectedPeers)
            {
                RelayCache.Add(inventory);
                foreach (RemoteNode node in connectedPeers)
                    relayed |= node.Relay(inventory);
            }
            return TR.Exit(relayed);
        }

        private void RelayDirectly(IReadOnlyCollection<Transaction> transactions)
        {
            TR.Enter();
            lock (connectedPeers)
            {
                foreach (RemoteNode node in connectedPeers)
                    node.Relay(transactions);
            }
            TR.Exit();
        }

        private void RemoteNode_Disconnected(object sender, bool error)
        {
            TR.Enter();
            RemoteNode remoteNode = (RemoteNode)sender;
            remoteNode.Disconnected -= RemoteNode_Disconnected;
            remoteNode.InventoryReceived -= RemoteNode_InventoryReceived;
            remoteNode.PeersReceived -= RemoteNode_PeersReceived;
            if (error && remoteNode.ListenerEndpoint != null)
            {
                lock (badPeers)
                {
                    badPeers.Add(remoteNode.ListenerEndpoint);
                }
            }
            lock (unconnectedPeers)
            {
                lock (connectedPeers)
                {
                    if (remoteNode.ListenerEndpoint != null)
                    {
                        unconnectedPeers.Remove(remoteNode.ListenerEndpoint);
                    }
                    connectedPeers.Remove(remoteNode);
                }
            }
            TR.Exit();
        }

        private void RemoteNode_InventoryReceived(object sender, IInventory inventory)
        {
            TR.Enter();
            if (inventory is Transaction tx && tx.Type != TransactionType.ClaimTransaction && tx.Type != TransactionType.IssueTransaction)
            {
                if (Blockchain.Default == null) return;
                if (!CheckKnownHashes(inventory.Hash)) return;
                InventoryReceivingEventArgs args = new InventoryReceivingEventArgs(inventory);
                InventoryReceiving?.Invoke(this, args);
                if (args.Cancel) return;
                lock (temp_pool)
                {
                    temp_pool.Add(tx);
                }
                new_tx_event.Set();
            }
            else
            {
                Relay(inventory);
            }
            TR.Exit();
        }

        private void RemoteNode_PeersReceived(object sender, IPEndPoint[] peers)
        {
            TR.Enter();
            lock (unconnectedPeers)
            {
                if (unconnectedPeers.Count < UnconnectedMax)
                {
                    lock (badPeers)
                    {
                        lock (connectedPeers)
                        {
                            unconnectedPeers.UnionWith(peers);
                            unconnectedPeers.ExceptWith(badPeers);
                            unconnectedPeers.ExceptWith(connectedPeers.Select(p => p.ListenerEndpoint));
                        }
                    }
                }
            }
            TR.Exit();
        }

        public IPEndPoint[] GetUnconnectedPeers()
        {
            TR.Enter();
            lock (unconnectedPeers)
            {
                return TR.Exit(unconnectedPeers.ToArray());
            }
        }

        public IPEndPoint[] GetBadPeers()
        {
            TR.Enter();
            lock (badPeers)
            {
                return TR.Exit(badPeers.ToArray());
            }
        }

        public static void SaveState(Stream stream)
        {
            TR.Enter();
            IPEndPoint[] peers;
            lock (unconnectedPeers)
            {
                peers = unconnectedPeers.Take(UnconnectedMax).ToArray();
            }
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
            {
                writer.Write(peers.Length);
                foreach (IPEndPoint endpoint in peers)
                {
                    writer.Write(endpoint.Address.MapToIPv4().GetAddressBytes());
                    writer.Write((ushort)endpoint.Port);
                }
            }
            TR.Exit();
        }

        public void Start(int port = 0, int ws_port = 0)
        {
            TR.Enter();
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                Task.Run(async () =>
                {
                    TR.Log("port : {0}, ws_port : {1}", port, ws_port);
                    if ((port > 0 || ws_port > 0)
                        && UpnpEnabled
                        && LocalAddresses.All(p => !p.IsIPv4MappedToIPv6 || IsIntranetAddress(p))
                        && await UPnP.DiscoverAsync())
                    {
                        TR.Log();
                        try
                        {
                            LocalAddresses.Add((await UPnP.GetExternalIPAsync()).MapToIPv6());
                            if (port > 0)
                                await UPnP.ForwardPortAsync(port, ProtocolType.Tcp, "NEO");
                            if (ws_port > 0)
                                await UPnP.ForwardPortAsync(ws_port, ProtocolType.Tcp, "NEO WebSocket");
                        }
                        catch
                        {
                            TR.Log();
                        }
                    }
                    connectThread.Start();
                    poolThread?.Start();
                    if (port > 0)
                    {
                        TR.Log("{0}", port);
                        listener = new TcpListener(IPAddress.Any, port);
                        listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                        try
                        {
                            listener.Start();
                            Port = (ushort)port;
                            IndentContext ic = TR.SaveContextAndShuffle();
                            AcceptPeers();
                            TR.RestoreContext(ic);
                        }
                        catch (SocketException) { }
                    }
                    if (ws_port > 0)
                    {
                        TR.Log("{0}", ws_port);
                        ws_host = new WebHostBuilder().UseKestrel().UseUrls($"http://*:{ws_port}").Configure(app => app.UseWebSockets().Run(ProcessWebSocketAsync)).Build();
                        ws_host.Start();
                    }
                    TR.Log();
                });
            }
            TR.Exit();
        }

        public void SynchronizeMemoryPool()
        {
            TR.Enter();
            lock (connectedPeers)
            {
                foreach (RemoteNode node in connectedPeers)
                    node.RequestMemoryPool();
            }
            TR.Exit();
        }
    }
}
