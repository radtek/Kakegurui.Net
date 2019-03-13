using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Net
{
    /// <summary>
    /// 客户端连入事件参数
    /// </summary>
    public class AcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// 监听套接字
        /// </summary>
        public Socket ListenSocket { get; set; }
        /// <summary>
        /// 客户端套接字
        /// </summary>
        public Socket AcceptSocket { get; set; }
        /// <summary>
        /// 处理实例
        /// </summary>
        public SocketHandler Handler { get; set; }
    }

    /// <summary>
    /// 关闭套接字事件参数
    /// </summary>
    public class ClosedEventArgs : EventArgs
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 套接字类型
        /// </summary>
        public SocketType Type { get; set; }
    }

    /// <summary>
    /// 确定客户端套接字编号事件参数
    /// </summary>
    public class TagConfirmedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }
        public ushort Tag { get; set; }
        public string LogName { get; set; }
    }

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
        public event EventHandler<AcceptedEventArgs> Accepted;

        /// <summary>
        /// 关闭套接字事件
        /// </summary>
        public event EventHandler<ClosedEventArgs> Closed;

        /// <summary>
        /// 线程集合
        /// </summary>
        protected readonly ConcurrentDictionary<TaskObject,object> _tasks = new ConcurrentDictionary<TaskObject,object>();

        /// <summary>
        /// 套接字集合
        /// </summary>
        protected readonly ConcurrentDictionary<Socket,SocketItem> _sockets=new ConcurrentDictionary<Socket, SocketItem>();

        /// <summary>
        /// 客户端套接字集合
        /// </summary>
        private readonly ConcurrentDictionary<ushort, SocketItem> _tags = new ConcurrentDictionary<ushort, SocketItem>();

        /// <summary>
        /// 连接地址集合
        /// </summary>
        private readonly ConcurrentDictionary<EndPoint, SocketItem> _endPoints = new ConcurrentDictionary<EndPoint, SocketItem>();

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

            Socket listenSocket = (Socket)sender;
            SocketItem listenItem = ((SocketItem)e.UserToken);
            AcceptedEventArgs args = new AcceptedEventArgs
            {
                ListenSocket = listenSocket,
                AcceptSocket = e.AcceptSocket,
                Handler = listenItem.Handler?.Clone()
            };

            SocketItem acceptItem = new SocketItem
            {
                Socket = e.AcceptSocket,
                Type = SocketType.Accept,
                Handler = args.Handler,
                StartTime = DateTime.Now,
                Tag = 0,
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
                Closed?.Invoke(this, new ClosedEventArgs
                {
                    Socket = item.Socket,
                    Type = item.Type
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
        /// 连接到服务事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConnectedEventHandler(object sender, ConnectedEventArgs e)
        {
            SocketItem item = new SocketItem
            {
                Socket = e.Socket,
                Type = SocketType.Connect,
                Handler = e.Handler,
                StartTime = DateTime.Now,
                RemoteEndPoint = e.RemoteEndPoint,
                LocalEndPoint = e.LocalEndPoint
            };

            LogPool.Logger.LogInformation("{0} {1} {2} {3}", "connected", e.Socket.Handle, item.RemoteEndPoint, item.LocalEndPoint);
            _endPoints[e.RemoteEndPoint] = item;
            _sockets[item.Socket] = item;

            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += ReceivedHandler;
            receiveArgs.SetBuffer(new byte[65536], 0, 65536);
            receiveArgs.AcceptSocket = item.Socket;
            receiveArgs.UserToken = item;
            if (!item.Socket.ReceiveAsync(receiveArgs))
            {
                ReceivedHandler(item.Socket, receiveArgs);
            }

        }

        /// <summary>
        /// 设置套接字标记
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="tag">套接字标记</param>
        /// <param name="logName">日志名</param>
        public void SetTag(Socket socket,ushort tag,string logName)
        {
            if (_sockets.TryGetValue(socket, out SocketItem item))
            {
                item.Tag = tag;
                _tags[tag] = item;
                item.Handler?.SetLogger(string.Format("{0}_{1}", logName, tag));
                LogPool.Logger.LogInformation("{0} {1} {2}", "tag", tag, socket.Handle);
            }
        }

        /// <summary>
        /// 添加监视线程
        /// </summary>
        /// <param name="task">线程</param>
        public void AddTask(TaskObject task)
        {
            _tasks[task]=null;
        }

        /// <summary>
        /// 移除监视线程
        /// </summary>
        /// <param name="task">线程</param>
        public void RemoveTask(TaskObject task)
        {
            _tasks.TryRemove(task, out object obj);
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
                Tag = 0,
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
            _connection.AddEndPoint(endPoint, handler);
        }

        /// <summary>
        /// 移除连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        public void RemoveConnectEndPoint(IPEndPoint endPoint)
        {
            if (_endPoints.TryRemove(endPoint, out SocketItem item))
            {
                if (item.Socket == null)
                {
                    LogPool.Logger.LogInformation("{0} {1}", "remove_connect", endPoint.ToString());
                }
                else
                {
                    RemoveSocket(item.Socket, "remove_connect");
                    _connection.RemoveEndPoint(endPoint);
                }
            }
        }

        /// <summary>
        /// 添加udp绑定地址
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">绑定端口，默认为0，表示绑定任意端口，此时表示udp客户端</param>
        /// <returns>udp客户端套接字</returns>
        public Socket AddBindEndPoint(SocketHandler handler, int port)
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
                Tag = 0,
                Type = SocketType.Udp,
                LocalEndPoint = endPoint,
                RemoteEndPoint = new IPEndPoint(0, 0)
            };

            _endPoints[endPoint] = item;
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

                //删除字典
                if (item.Type == SocketType.Accept)
                {
                    if (_tags.TryRemove(item.Tag, out SocketItem i1))
                    {
                        i1.Tag = 0;
                    }
                }
                else if (item.Type == SocketType.Connect)
                {
                    if (_endPoints.TryGetValue(item.RemoteEndPoint, out SocketItem i2))
                    {
                        i2.Socket = null;
                    }

                    _connection.ReportError(item.RemoteEndPoint);
                }
            }
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
            if (_sockets.TryGetValue(socket, out SocketItem item))
            {
                return item.Handler?.SendTcp(socket, buffer, handler) ?? SocketResult.NotFoundHandler;
            }
            else
            {
                return SocketResult.NotFoundHandler;
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">连入套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(ushort tag, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            return _tags.TryGetValue(tag, out SocketItem item)
                ? SendTcp(item.Socket, buffer, handler)
                : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="endPoint">发送地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(IPEndPoint endPoint, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            if (_endPoints.TryGetValue(endPoint, out SocketItem item))
            {
                return item.Socket == null ? SocketResult.Disconnection : SendTcp(item.Socket, buffer, handler);
            }
            else
            {
                return SocketResult.NotFoundEndPoint;
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
        /// tcp发送，并等待返回结果
        /// </summary>
        /// <param name="endPoint">发送地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="protocolId">等待协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="receiveBuffer">用于放置接收到的字节流的缓冲，默认为null即不需要记录接收字节流</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(IPEndPoint endPoint, List<byte> buffer, int protocolId, long timeStamp = 0, List<byte> receiveBuffer = null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendTcp(endPoint, buffer, handler);
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
        /// udp发送
        /// </summary>
        /// <param name="udpSocket">udp套接字</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(Socket udpSocket, IPEndPoint remoteEndPoint, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            if (_sockets.TryGetValue(udpSocket, out SocketItem item))
            {
                return item.Handler?.SendUdp(udpSocket, remoteEndPoint, buffer, handler) ?? SocketResult.NotFoundHandler;
            }
            else
            {
                return SocketResult.NotFoundHandler;
            }
        }

        /// <summary>
        /// udp发送
        /// </summary>
        /// <param name="bindEndPoint">本地绑定地址</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(IPEndPoint bindEndPoint, IPEndPoint remoteEndPoint, List<byte> buffer, ReceiveAsyncHandler handler = null)
        {
            if (_endPoints.TryGetValue(bindEndPoint, out SocketItem item))
            {
                return item.Socket == null
                    ? SocketResult.Disconnection
                    : SendUdp(item.Socket, remoteEndPoint, buffer, handler);
            }
            else
            {
                return SocketResult.NotFoundEndPoint;
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
        /// tcp通知
        /// </summary>
        /// <param name="buffer">字节流</param>
        /// <param name="port">服务监听端口</param>
        /// <returns>发送结果</returns>
        public void Notice(List<byte> buffer, int port=0)
        {
            foreach (var pair in _sockets)
            {
                if (pair.Value.Type == SocketType.Accept)
                {
                    if (port == 0 || (pair.Value.LocalEndPoint).Port == port)
                    {
                        pair.Value.Handler?.SendTcp(pair.Key, buffer);
                    }
                }
            }
        }

        protected override void ActionCore()
        {
            AddTask(this);
            AddTask(_connection);
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
                            builder.AppendFormat("{0} local:{1} remote:{2} tag:{3} t:{4} r:{5}\n",
                                socket.Key.Handle,
                                socket.Value.LocalEndPoint,
                                socket.Value.RemoteEndPoint,
                                socket.Value.Tag,
                                socket.Value.Handler?.TransmitSize,
                                socket.Value.Handler?.ReceiveSize);
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
                    builder.Append("thread:\n");

                    foreach (var task in _tasks)
                    {
                        builder.AppendFormat("{0} {1}\n", task.Key.Name, task.Key.HitPoint.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    }
                    LogPool.Logger.LogTrace(builder.ToString());
                }

                ++monitorPollIndex;
                Thread.Sleep(1000);
            }
            _connection.Stop();
            foreach (var pair in _sockets)
            {
                pair.Value.Socket.Close();
            }
        }
    }
}
