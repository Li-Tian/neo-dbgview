using DbgViewTR;
using Neo.IO;
using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    internal class WebSocketRemoteNode : RemoteNode
    {
        private WebSocket socket;
        private bool connected = false;
        private int disposed = 0;

        public WebSocketRemoteNode(LocalNode localNode, WebSocket socket, IPEndPoint remoteEndpoint)
            : base(localNode)
        {
            TR.Enter();
            this.socket = socket;
            this.RemoteEndpoint = new IPEndPoint(remoteEndpoint.Address.MapToIPv6(), remoteEndpoint.Port);
            this.connected = true;
            TR.Exit();
        }

        public override void Disconnect(bool error)
        {
            TR.Enter();
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                TR.Log();
                socket.Dispose();
                base.Disconnect(error);
            }
            TR.Exit();
        }

        protected override async Task<Message> ReceiveMessageAsync(TimeSpan timeout)
        {
            TR.Enter();
            CancellationTokenSource source = new CancellationTokenSource(timeout);
            try
            {
                Message msg = await Message.DeserializeFromAsync(socket, source.Token);
                return TR.Exit(msg);
            }
            catch (ArgumentException) { TR.Log(); }
            catch (ObjectDisposedException) { TR.Log(); }
            catch (Exception ex) when (ex is FormatException || ex is IOException || ex is WebSocketException || ex is OperationCanceledException)
            {
                TR.Log();
                Disconnect(false);
            }
            finally
            {
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
            ArraySegment<byte> segment = new ArraySegment<byte>(message.ToArray());
            CancellationTokenSource source = new CancellationTokenSource(10000);
            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Binary, true, source.Token);
                return TR.Exit(true);
            }
            catch (ObjectDisposedException) { TR.Log(); }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException)
            {
                TR.Log();
                Disconnect(false);
            }
            finally
            {
                TR.Log();
                source.Dispose();
            }
            return TR.Exit(false);
        }
    }
}
