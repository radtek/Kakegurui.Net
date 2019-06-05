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
        /// <summary>
        /// 数据包
        /// </summary>
        public List<byte> Packet { get; set; }

        /// <summary>
        /// 服务地址
        /// </summary>
        public string Ip { get; set; } 

        /// <summary>
        /// 服务端口
        /// </summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// ws客户端通道
    /// </summary>
    public class WebSocketClientChannel : TaskObject
    {

        /// <summary>
        /// 重连时间
        /// </summary>
        private const int ConnectionSpan = 10 * 1000;

        /// <summary>
        /// ws服务url
        /// </summary>
        public Uri Url { get; }

        /// <summary>
        /// 是否连接到ws服务
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">ws服务url</param>
        public WebSocketClientChannel(string url) 
            : base("ws_client")
        {
            Url = new Uri(url);
        }

        /// <summary>
        /// 接收到ws数据事件
        /// </summary>
        public event EventHandler<WebSocketReceivedEventArges> WebSocketReceived;

        protected override void ActionCore()
        {

            while (!IsCancelled())
            {
                Connected = false;
                using (ClientWebSocket webSocket = new ClientWebSocket())
                {
                    try
                    {
                        Task connectTask = webSocket.ConnectAsync(Url, _token);
                        connectTask.Wait(_token);
                    }
                    catch (AggregateException)
                    {
                        Thread.Sleep(ConnectionSpan);
                        continue;
                    }
                    catch (OperationCanceledException)
                    {
                        webSocket.Abort();
                        break;
                    }

                    LogPool.Logger.LogInformation("ws_connect {0}", Url);
                    Connected = true;
                    byte[] buffer = new byte[10 * 1024];
                    List<byte> packet = new List<byte>();
                    while (!IsCancelled())
                    {
                        try
                        {
                            Task<WebSocketReceiveResult> receiveTask =
                                webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _token);
                            receiveTask.Wait(_token);
                            packet.AddRange(buffer.Take(receiveTask.Result.Count));
                            if (receiveTask.Result.EndOfMessage)
                            {
                                WebSocketReceived?.Invoke(this, new WebSocketReceivedEventArges
                                {
                                    Packet = packet,
                                    Ip = Url.Host,
                                    Port = Url.Port
                                });
                                packet.Clear();
                            }
                        }
                        catch (AggregateException)
                        {
                            LogPool.Logger.LogInformation("ws_shutdown {0}", Url);
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            webSocket.Abort();
                            break;
                        }
                    }
                }
            }
        }
    }
}
