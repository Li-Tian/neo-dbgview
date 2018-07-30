using DbgViewTR;
using Neo.IO;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    internal class TcpRemoteNode : RemoteNode
    {
        private Socket socket;
        private NetworkStream stream;
        private bool connected = false;
        private int disposed = 0;

        public TcpRemoteNode(LocalNode localNode, IPEndPoint remoteEndpoint)
            : base(localNode)
        {
            TR.Enter();
            this.socket = new Socket(remoteEndpoint.Address.IsIPv4MappedToIPv6 ? AddressFamily.InterNetwork : remoteEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.ListenerEndpoint = remoteEndpoint;
            TR.Exit();
        }

        public TcpRemoteNode(LocalNode localNode, Socket socket)
            : base(localNode)
        {
            TR.Enter();
            this.socket = socket;
            OnConnected();
            TR.Exit();
        }

        public async Task<bool> ConnectAsync()
        {
            TR.Enter();
            IPAddress address = ListenerEndpoint.Address;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            try
            {
                TR.Log(ListenerEndpoint.ToString());
                IndentContext ic = TR.SaveContextAndShuffle();
                await socket.ConnectAsync(address, ListenerEndpoint.Port);
                TR.RestoreContext(ic);
                TR.Log();
                OnConnected();
                TR.Log();
            }
            catch (SocketException)
            {
                TR.Log();
                Disconnect(false);
                return TR.Exit(false);
            }
            return TR.Exit(true);
        }

        public override void Disconnect(bool error)
        {
            TR.Enter();
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                if (stream != null) stream.Dispose();
                socket.Dispose();
                base.Disconnect(error);
            }
            TR.Exit();
        }

        private void OnConnected()
        {
            TR.Enter();
            IPEndPoint remoteEndpoint = (IPEndPoint)socket.RemoteEndPoint;
            RemoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            stream = new NetworkStream(socket);
            connected = true;
            TR.Exit();
        }

        protected override async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            await Task.Yield();
            TR.Enter();
            CancellationTokenSource source = new CancellationTokenSource(timeout);
            //Stream.ReadAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => Disconnect(false));
            IndentContext ic = TR.SaveContextAndShuffle();
            try
            {
                Message msg = await Message.DeserializeFromAsync(stream, source.Token);
                TR.RestoreContext(ic);
                return TR.Exit(msg);
            }
            catch (ArgumentException) { TR.Log(); }
            catch (ObjectDisposedException) { TR.Log(); }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is OperationCanceledException)
            {
                TR.Log();
                Disconnect(false);
            }
            finally
            {
                TR.RestoreContext(ic);
                TR.Log();
                source.Dispose();
            }
            return TR.Exit((Message)null);
        }

        protected override async Task<bool> SendMessageAsync(Message message)
        {
            TR.Enter();
            if (!connected) throw new InvalidOperationException();
            if (disposed > 0) return TR.Exit(false);
            byte[] buffer = message.ToArray();
            CancellationTokenSource source = new CancellationTokenSource(30000);
            //Stream.WriteAsync doesn't support CancellationToken
            //see: https://stackoverflow.com/questions/20131434/cancel-networkstream-readasync-using-tcplistener
            source.Token.Register(() => Disconnect(false));
            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, source.Token);
                TR.Log("message : {0} sent to {1}", message.Command, RemoteEndpoint.Address);
                return TR.Exit(true);
            }
            catch (ObjectDisposedException)
            {
                TR.Log();
            }
            catch (Exception ex) when (ex is IOException || ex is OperationCanceledException)
            {
                Disconnect(false);
            }
            finally
            {
                source.Dispose();
            }
            return TR.Exit(false);
        }
    }
}
