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
        /// 添加tcp监听端口
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
        /// 添加udp绑定端口
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
        /// 套接字
        /// </summary>
        /// <param name="item">套接字详情</param>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="action">成功响应后的异步回调，默认为null，表示同步等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        private SocketResult Send(SocketItem item, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match = null, Action<SocketPack> action=null,List<byte> receiveBuffer = null,int timeout = 3000)
        {
            return item.Handler?.Send(item.Socket, remoteEndPoint, buffer,match, action, receiveBuffer, timeout) ?? SocketResult.NotFoundHandler;
        }

        /// <summary>
        /// tcp发送，同步响应
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(Socket socket, List<byte> buffer, Func<SocketPack, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketItem item) ? Send(item,null, buffer, match, null,receiveBuffer, timeout) : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送，异步响应
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="action">成功响应后的异步回调，表示同步等待响应</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcpAsync(Socket socket, List<byte> buffer, Func<SocketPack, bool> match, Action<SocketPack> action, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketItem item) ? Send(item,null, buffer, match, action,null, timeout) : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送，同步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(string tag, List<byte> buffer,Func<SocketPack,bool> match=null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key==null? SocketResult.NotFoundSocket: Send(socket.Value,null, buffer, match, null,receiveBuffer, timeout);
        }

        /// <summary>
        /// tcp发送，异步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="action">成功响应后的异步回调，表示同步等待响应</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcpAsync(string tag, List<byte> buffer, Func<SocketPack, bool> match , Action<SocketPack> action, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key == null ? SocketResult.NotFoundSocket : Send(socket.Value,null, buffer, match, action, null, timeout);
        }

        /// <summary>
        /// tcp广播
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        public List<SocketResult> Broadcast(string tag, List<byte> buffer)
        {
            return _sockets
                .Where(s => s.Value.Tag == tag)
                .Select(s => Send(s.Value,null, buffer))
                .ToList();
        }

        /// <summary>
        /// tcp发送，同步响应
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">udp远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketItem item) ? Send(item, remoteEndPoint, buffer, match, null, receiveBuffer, timeout) : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送，异步响应
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">udp远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="action">成功响应后的异步回调，表示同步等待响应</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdpAsync(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match, Action<SocketPack> action, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketItem item) ? Send(item, remoteEndPoint, buffer, match, action, null, timeout) : SocketResult.NotFoundSocket;
        }


        /// <summary>
        /// tcp发送，同步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key == null ? SocketResult.NotFoundSocket : Send(socket.Value, remoteEndPoint, buffer, match, null, receiveBuffer, timeout);
        }

        /// <summary>
        /// tcp发送，异步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址</param> 
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="action">成功响应后的异步回调，表示同步等待响应</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdpAsync(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match, Action<SocketPack> action, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key == null ? SocketResult.NotFoundSocket : Send(socket.Value, remoteEndPoint, buffer, match, action, null, timeout);
        }

        /// <summary>
        /// udp广播
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址</param> 
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        public IEnumerable<SocketResult> Broadcast(string tag, IPEndPoint remoteEndPoint, List<byte> buffer)
        {
            return _sockets.AsParallel()
                .Where(s => s.Value.Tag == tag)
                .Select(s => Send(s.Value, remoteEndPoint, buffer));
        }

        protected override void ActionCore()
        {
            _connection.Start();
            int monitorPoll = 0;
            int monitorSpan = AppConfig.ReadInt32("MonitorSpan") ?? 60;
            while (!IsCancelled())
            {
                if (monitorPoll % monitorSpan == 0)
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

                ++monitorPoll;
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
