using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;

namespace Kakegurui.Net
{
    /// <summary>
    /// websocket服务端
    /// </summary>
    public class WebSockerServerChannel : TaskObject
    {
        /// <summary>
        /// 客户端集合
        /// </summary>
        private readonly ConcurrentDictionary<WebSocket, object> _clients = new ConcurrentDictionary<WebSocket, object>();

        /// <summary>
        /// 服务端监听的url
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">服务端监听的url</param>
        public WebSockerServerChannel(string url)
            : base("websocket_server")
        {
            _url = url;
        }

        /// <summary>
        /// 向所有客户端广播数据
        /// </summary>
        /// <param name="buffer">数据字节流</param>
        public void Broadcast(byte[] buffer)
        {
            foreach (var client in _clients)
            {
                client.Key.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        protected override void ActionCore()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(_url);
            listener.Start();

            while (!IsCancelled())
            {
                var context = listener.GetContext();
                Task<HttpListenerWebSocketContext> wsContext = context.AcceptWebSocketAsync(null);

                wsContext.Wait(_token);
                WebSocket client = wsContext.Result.WebSocket;
                _clients.TryAdd(client, null);
            }
            foreach (KeyValuePair<WebSocket, object> pair in _clients)
            {
                pair.Key.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
                pair.Key.Dispose();
            }
            listener.Stop();
        }

    }
}
