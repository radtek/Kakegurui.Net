using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Net
{
    /// <summary>
    /// 接收到ws数据
    /// </summary>
    public class WebSocketReceivedEventArges : EventArgs
    {
        public List<byte> Packet { get; set; }
    }

    /// <summary>
    /// ws客户端通道
    /// </summary>
    public class WebSocketClientChannel : TaskObject
    {
        /// <summary>
        /// ws服务url
        /// </summary>
        private readonly string _url;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">ws服务url</param>
        public WebSocketClientChannel(string url) 
            : base("ws_client")
        {
            _url = url;
        }

        /// <summary>
        /// 接收到ws数据事件
        /// </summary>
        public event EventHandler<WebSocketReceivedEventArges> WebSocketReceived;

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                ClientWebSocket webSocket = new ClientWebSocket();
                try
                {
                    Task connectTask = webSocket.ConnectAsync(new Uri(_url), _token);
                    connectTask.Wait(_token);
                }
                catch (AggregateException)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(AppConfig.ConnectionSpan));
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                LogPool.Logger.LogInformation("connect {0}", _url);
                byte[] buffer = new byte[10 * 1024];
                List<byte> packet = new List<byte>();
                while (!_token.IsCancellationRequested)
                {
                    try
                    {
                        Task<WebSocketReceiveResult> receiveTask =
                            webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        receiveTask.Wait(_token);
                        packet.AddRange(buffer.Take(receiveTask.Result.Count));
                        if (receiveTask.Result.EndOfMessage)
                        {
                            WebSocketReceived?.Invoke(this, new WebSocketReceivedEventArges { Packet = packet });
                            packet.Clear();
                        }
                    }
                    catch (AggregateException)
                    {
                        LogPool.Logger.LogInformation("close", _url);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
    }
}
