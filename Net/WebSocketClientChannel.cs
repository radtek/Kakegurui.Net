using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        /// ws服务url
        /// </summary>
        private readonly Uri _url;

        /// <summary>
        /// ws服务url
        /// </summary>
        public Uri Url => _url;

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
            _url = new Uri(url);
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
                ClientWebSocket webSocket = new ClientWebSocket();
                try
                {
                    Task connectTask = webSocket.ConnectAsync(_url, _token);
                    connectTask.Wait(_token);
                }
                catch (AggregateException)
                {
                    continue;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                LogPool.Logger.LogInformation("ws_connect {0}", _url);
                Connected = true;
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
                            WebSocketReceived?.Invoke(this, new WebSocketReceivedEventArges
                            {
                                Packet = packet,
                                Ip = _url.Host,
                                Port = _url.Port
                            });
                            packet.Clear();
                        }
                    }
                    catch (AggregateException)
                    {
                        LogPool.Logger.LogInformation("ws_shutdown {0}", _url);
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
