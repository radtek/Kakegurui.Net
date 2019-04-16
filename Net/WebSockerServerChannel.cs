using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

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
        /// <param name="port">监听端口</param>
        /// <param name="url">监听url</param>
        public WebSockerServerChannel(int port,string url)
            : base("websocket_server")
        {
            _url = $"http://+:{port}/{url}";
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
            LogPool.Logger.LogInformation("ws_listen {0}",_url);
            listener.Start();
            while (!IsCancelled())
            {
                var context = listener.GetContext();
                Task<HttpListenerWebSocketContext> wsContext = context.AcceptWebSocketAsync(null);
                wsContext.Wait(_token);
                WebSocket client = wsContext.Result.WebSocket;
                _clients.TryAdd(client, null);
                async Task Function()
                {
                    var buffer = new byte[0];
                    WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    while (!result.CloseStatus.HasValue)
                    {
                        result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    }
                    await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    LogPool.Logger.LogInformation("ws_close {0}", _url);

                    _clients.TryRemove(client, out object obj);
                }
                Task.Run(Function);
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
