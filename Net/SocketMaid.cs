using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Net
{
    /// <summary>
    /// 套接字通道
    /// </summary>
    public class SocketMaid:TaskObject
    {
        /// <summary>
        /// 缓冲容量
        /// </summary>
        private const int BufferLength = 65536;

        /// <summary>
        /// 客户端连入事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Accepted;

        /// <summary>
        /// 连入到服务端事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Connected;

        /// <summary>
        /// 关闭套接字事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Closed;

        /// <summary>
        /// 套接字集合
        /// </summary>
        protected readonly ConcurrentDictionary<Socket,SocketItem> _sockets=new ConcurrentDictionary<Socket, SocketItem>();

        /// <summary>
        /// 连接线程
        /// </summary>
        private readonly ConnectionTask _connection=new ConnectionTask();

        /// <summary>
        /// 构造函数
        /// </summary>
        public SocketMaid()
            : this("socket_maid")
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">线程名</param>
        public SocketMaid(string name)
            :base(name)
        {
            _connection.Connected += ConnectedEventHandler;
        }

        /// <summary>
        /// 连接到服务事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConnectedEventHandler(object sender, SocketEventArgs e)
        {
            LogPool.Logger.LogInformation("{0} {1} {2} {3}", "connected", e.Item.Socket.Handle, e.Item.RemoteEndPoint, e.Item.LocalEndPoint);
            _sockets[e.Item.Socket] = e.Item;

            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += ReceivedHandler;
            receiveArgs.SetBuffer(new byte[65536], 0, 65536);
            receiveArgs.AcceptSocket = e.Item.Socket;
            receiveArgs.UserToken = e.Item;
            if (!e.Item.Socket.ReceiveAsync(receiveArgs))
            {
                ReceivedHandler(e.Item.Socket, receiveArgs);
            }
            Connected?.Invoke(this,e);
        }

        /// <summary>
        /// 等待套接字事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptedEventHandler(object sender, SocketAsyncEventArgs e)
        {
            SocketItem item= (SocketItem)e.UserToken;
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += AcceptedEventHandler;
            acceptArgs.UserToken = item;
            if (!item.Socket.AcceptAsync(acceptArgs))
            {
                AcceptedEventHandler(item.Socket, acceptArgs);
            }

            SocketItem listenItem = ((SocketItem)e.UserToken);
            SocketItem acceptItem = new SocketItem
            {
                Socket = e.AcceptSocket,
                Type = SocketType.Accept,
                Handler = listenItem.Handler?.Clone(),
                StartTime = DateTime.Now,
                Tag = ((IPEndPoint)e.AcceptSocket.LocalEndPoint).Port.ToString(),
                LocalEndPoint = (IPEndPoint)e.AcceptSocket.LocalEndPoint,
                RemoteEndPoint = (IPEndPoint)e.AcceptSocket.RemoteEndPoint
            };

            LogPool.Logger.LogInformation("{0} {1} {2} {3}", "accepted", e.AcceptSocket.Handle, acceptItem.RemoteEndPoint, acceptItem.LocalEndPoint);
            _sockets[e.AcceptSocket] = acceptItem;
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += ReceivedHandler;
            receiveArgs.SetBuffer(new byte[BufferLength], 0, BufferLength);
            receiveArgs.AcceptSocket = acceptItem.Socket;
            receiveArgs.UserToken = acceptItem;
            if (!acceptItem.Socket.ReceiveAsync(receiveArgs))
            {
                ReceivedHandler(acceptItem.Socket, receiveArgs);
            }

            SocketEventArgs args = new SocketEventArgs
            {
                Item=acceptItem
            };
            Accepted?.Invoke(this, args);
        }

        /// <summary>
        /// 接收事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceivedHandler(object sender, SocketAsyncEventArgs e)
        {
            Socket socket = (Socket)sender;
            SocketItem item = (SocketItem)e.UserToken;
            if (e.BytesTransferred == 0)
            {
                RemoveSocket(item.Socket, "closed");
                Closed?.Invoke(this, new SocketEventArgs
                {
                   Item=item
                });
            }
            else
            {
                if (item.Type == SocketType.Udp)
                {
                    item.Handler?.Handle(socket, e.Buffer, e.BytesTransferred, (IPEndPoint)e.RemoteEndPoint);
                    try
                    {
                        if (!socket.ReceiveFromAsync(e))
                        {
                            ReceivedHandler(socket, e);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }                  
                }
                else
                {
                    item.Handler?.Handle(socket, e.Buffer, e.BytesTransferred);
                    try
                    {
                        if (!socket.ReceiveAsync(e))
                        {
                            ReceivedHandler(socket, e);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 添加监听地址
        /// </summary>
        /// <param name="port">监听端口</param>
        /// <param name="handler">处理实例</param>
        public void AddListenEndPoint(int port, SocketHandler handler)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            try
            {
                socket.Bind(endPoint);
                socket.Listen(10);
            }
            catch (SocketException e)
            {
                socket.Close();
                LogPool.Logger.LogInformation(e,"{0} {1}", "listen_error", endPoint.ToString());
                return;
            }
            LogPool.Logger.LogInformation("{0} {1} {2}", "listen", socket.Handle, endPoint.ToString());
            SocketItem item = new SocketItem
            {
                Handler = handler,
                Socket = socket,
                StartTime = DateTime.Now,
                Tag = null,
                Type = SocketType.Listen,
                LocalEndPoint = endPoint,
                RemoteEndPoint = new IPEndPoint(0,0)
            };
            _sockets[socket] = item;
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += AcceptedEventHandler;
            acceptArgs.UserToken = item;
            if (!item.Socket.AcceptAsync(acceptArgs))
            {
                AcceptedEventHandler(item.Socket, acceptArgs);
            }
        }

        /// <summary>
        /// 添加连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        /// <param name="handler">处理实例</param>
        public void AddConnectEndPoint(IPEndPoint endPoint, SocketHandler handler)
        {
            LogPool.Logger.LogInformation("{0} {1}", "add_connect", endPoint.ToString());
            _connection.AddEndPoint(new SocketItem
            {
                RemoteEndPoint = endPoint,
                Tag = endPoint.ToString(),
                Handler = handler
            });
        }

        /// <summary>
        /// 添加udp绑定地址
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">绑定端口，默认为0，表示绑定任意端口，此时表示udp客户端</param>
        /// <returns>udp客户端套接字</returns>
        public Socket AddBindEndPoint(SocketHandler handler, int port=0)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endPoint= new IPEndPoint(IPAddress.Any, port);
            try
            {
                socket.Bind(endPoint);
            }
            catch (SocketException e)
            {
                socket.Close();
                LogPool.Logger.LogInformation(e,"{0} {1}", "udpclient_error", socket.Handle);
                return null;
            }

            LogPool.Logger.LogInformation("{0} {1} {2}", "bind", socket.Handle, endPoint.ToString());

            SocketItem item = new SocketItem
            {
                Handler = handler,
                Socket = socket,
                StartTime = DateTime.Now,
                Type = SocketType.Udp,
                LocalEndPoint = endPoint,
                Tag = endPoint.ToString(),
                RemoteEndPoint = new IPEndPoint(0, 0)
            };

            _sockets[item.Socket] = item;

            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += ReceivedHandler;
            receiveArgs.SetBuffer(new byte[BufferLength], 0, BufferLength);
            receiveArgs.RemoteEndPoint = item.LocalEndPoint;
            receiveArgs.UserToken = item;
            if (!item.Socket.ReceiveFromAsync(receiveArgs))
            {
                ReceivedHandler(item.Socket, receiveArgs);
            }

            return socket;
        }

        /// <summary>
        /// 移除套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="reason">关闭套接字原因</param>
        protected void RemoveSocket(Socket socket,string reason)
        {
            if (_sockets.TryRemove(socket, out SocketItem item))
            {
                LogPool.Logger.LogInformation(
                    "{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                    reason,
                    socket.Handle,
                    item.LocalEndPoint?.ToString(),
                    item.RemoteEndPoint?.ToString(),
                    item.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    item.Tag,
                    item.Type,
                    item.Handler?.TransmitSize,
                    item.Handler?.ReceiveSize);

                item.Socket.Close();

                if (item.Type == SocketType.Connect)
                {
                    _connection.ReportError(item);
                }
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="item">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        private SocketResult SendTcp(SocketItem item, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            return item.Handler?.SendTcp(item.Socket, buffer, handler) ?? SocketResult.NotFoundHandler;
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(Socket socket, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            if (socket == null)
            {
                return SocketResult.NotFoundSocket;
            }
            else
            {
                return _sockets.TryGetValue(socket, out SocketItem item)
                    ? SendTcp(item, buffer, handler)
                    : SocketResult.NotFoundSocket;
            }
        }

        /// <summary>
        /// tcp发送，并等待返回结果
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="protocolId">等待协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="receiveBuffer">用于放置接收到的字节流的缓冲，默认为null即不需要记录接收字节流</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(Socket socket, List<byte> buffer, int protocolId, long timeStamp = 0, List<byte> receiveBuffer = null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendTcp(socket, buffer, handler);
            if (result == SocketResult.Success)
            {
                result = handler.Wait() ? SocketResult.Success : SocketResult.Timeout;
                if (result == SocketResult.Success)
                {
                    receiveBuffer?.AddRange(handler.Buffer);
                }
                return result;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler"></param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(string tag, List<byte> buffer, ReceiveAsyncHandler handler)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key == null ? SocketResult.NotFoundSocket : SendTcp(socket.Value, buffer, handler);
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="protocolId">等待协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="receiveBuffer">用于放置接收到的字节流的缓冲，默认为null即不需要记录接收字节流</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(string tag, List<byte> buffer, int protocolId, long timeStamp = 0, List<byte> receiveBuffer = null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendTcp(tag, buffer, handler);
            if (result == SocketResult.Success)
            {
                result = handler.Wait() ? SocketResult.Success : SocketResult.Timeout;
                if (result == SocketResult.Success)
                {
                    receiveBuffer?.AddRange(handler.Buffer);
                }
                return result;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        public List<SocketResult> SendTcp(string tag, List<byte> buffer)
        {
            return _sockets
                .Where(s => s.Value.Tag == tag)
                .Select(s => SendTcp(s.Value, buffer))
                .ToList();
        }

        /// <summary>
        /// udp发送
        /// </summary>
        /// <param name="item">udp套接字</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        private SocketResult SendUdp(SocketItem item, IPEndPoint remoteEndPoint, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            return item.Handler?.SendUdp(item.Socket, remoteEndPoint, buffer, handler) ?? SocketResult.NotFoundHandler;
        }

        /// <summary>
        /// udp发送
        /// </summary>
        /// <param name="udpSocket">udp套接字</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(Socket udpSocket, IPEndPoint remoteEndPoint, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            if (udpSocket == null)
            {
                return SocketResult.NotFoundSocket;
            }
            else
            {
                return _sockets.TryGetValue(udpSocket, out SocketItem item) ? SendUdp(item, remoteEndPoint, buffer, handler) : SocketResult.NotFoundSocket;
            }
        }

        /// <summary>
        /// udp发送，并等待返回结果
        /// </summary>
        /// <param name="udpSocket">udp套接字</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="protocolId">等待协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="receiveBuffer">用于放置接收到的字节流的缓冲，默认为null即不需要记录接收字节流</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(Socket udpSocket, IPEndPoint remoteEndPoint, List<byte> buffer, int protocolId, long timeStamp = 0, List<byte> receiveBuffer = null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendUdp(udpSocket, remoteEndPoint, buffer, handler);
            if (result == SocketResult.Success)
            {
                result = handler.Wait() ? SocketResult.Success : SocketResult.Timeout;
                if (result == SocketResult.Success)
                {
                    receiveBuffer?.AddRange(handler.Buffer);
                }
                return result;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// udp发送，并等待返回结果
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, ReceiveAsyncHandler handler)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key == null ? SocketResult.NotFoundSocket : SendUdp(socket.Value, remoteEndPoint, buffer, handler);
        }

        /// <summary>
        /// udp发送，并等待返回结果
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="protocolId">等待协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="receiveBuffer">用于放置接收到的字节流的缓冲，默认为null即不需要记录接收字节流</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, int protocolId, long timeStamp = 0, List<byte> receiveBuffer = null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendUdp(tag, remoteEndPoint, buffer, handler);
            if (result == SocketResult.Success)
            {
                result = handler.Wait() ? SocketResult.Success : SocketResult.Timeout;
                if (result == SocketResult.Success)
                {
                    receiveBuffer?.AddRange(handler.Buffer);
                }
                return result;
            }
            else
            {
                return result;
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">连入套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        public IEnumerable<SocketResult> SendUdp(string tag, IPEndPoint remoteEndPoint, List<byte> buffer)
        {
            return _sockets.AsParallel()
                .Where(s => s.Value.Tag == tag)
                .Select(s => SendUdp(s.Value, remoteEndPoint, buffer));
        }

        protected override void ActionCore()
        {
            _connection.Start();
            int monitorPollIndex = 0;
            while (!IsCancelled())
            {
                if (monitorPollIndex % 60 == 0)
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("socket:\n");
                    foreach (var socket in _sockets)
                    {
                        try
                        {
                            builder.AppendFormat("{0} local:{1} remote:{2} tag:{3} t:{4} r:{5} time:{6:yyyy-MM-dd HH:mm:ss.fff}\n",
                                socket.Key.Handle,
                                socket.Value.LocalEndPoint,
                                socket.Value.RemoteEndPoint,
                                socket.Value.Tag,
                                socket.Value.Handler?.TransmitSize,
                                socket.Value.Handler?.ReceiveSize, socket.Value.StartTime);
                        }
                        catch (SocketException)
                        {
                            builder.AppendFormat("{0} local:{1} tag:{2} t:{3} r:{4}\n",
                                socket.Key.Handle,
                                socket.Value.LocalEndPoint,
                                socket.Value.Tag,
                                socket.Value.Handler?.TransmitSize,
                                socket.Value.Handler?.ReceiveSize);
                        }
                    }
                    LogPool.Logger.LogTrace(builder.ToString());
                }

                ++monitorPollIndex;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            _connection.Stop();
            foreach (var pair in _sockets)
            {
                pair.Value.Socket.Close();
            }
        }
    }
}
