using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Kakegurui.Core;
using Kakegurui.Net;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 协议收发
    /// </summary>
    public class ProtocolMaid:TaskObject
    {
        /// <summary>
        /// 连接线程
        /// </summary>
        private readonly EndPointChannel _connection=new EndPointChannel();

        /// <summary>
        /// 接收线程
        /// </summary>
        private readonly SocketChannel _channel=new SocketChannel();

        /// <summary>
        /// 线程集合
        /// </summary>
        private readonly ConcurrentBag<TaskObject> _tasks=new ConcurrentBag<TaskObject>();

        /// <summary>
        /// 套接字集合
        /// </summary>
        protected readonly ConcurrentDictionary<Socket,SocketItem> _sockets=new ConcurrentDictionary<Socket, SocketItem>();

        /// <summary>
        /// 客户端套接字集合
        /// </summary>
        private readonly ConcurrentDictionary<ushort, Socket> _tags=new ConcurrentDictionary<ushort, Socket>();

        /// <summary>
        /// 监听，绑定，连接套接字集合
        /// </summary>
        private readonly ConcurrentDictionary<IPEndPoint,Socket> _endPoints=new ConcurrentDictionary<IPEndPoint, Socket>();

        /// <summary>
        /// 订阅集合
        /// </summary>
        private readonly LinkedList<Subscribe_Request> _subscribes = new LinkedList<Subscribe_Request>();

        /// <summary>
        /// 监控日志
        /// </summary>
        private readonly Logger _logger;

        /// <summary>
        /// 轮询序号
        /// </summary>
        private int _pollIndex;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProtocolMaid()
            :base("protocolmaid")
        {
            _connection.Connected += ConnectedEventHandler;
            _channel.Accepted += AcceptedEventHandler;
            _channel.Closed += ClosedEventHandler;
            _tasks.Add(_connection);
            _tasks.Add(_channel);
            _logger=new FileLogger(new AllFilter(), "Monitor");
        }

        /// <summary>
        /// 添加监听地址
        /// </summary>
        /// <param name="endPoint">监听地址</param>
        /// <param name="handler">处理实例</param>
        public void AddListenEndPoint(IPEndPoint endPoint, SocketHandler handler)
        { 
            Socket socket=new Socket(AddressFamily.InterNetwork,System.Net.Sockets.SocketType.Stream,ProtocolType.Tcp);
            socket.Bind(endPoint);
            socket.Listen(10);
            LogPool.Logger.LogInformation("{0} {1} {2}","listen", socket.Handle, endPoint.ToString());
            _channel.AddSocket(socket, Net.SocketType.Listen,handler);
            SocketItem item = new SocketItem
            {
                Handler = handler,
                Socket = socket,
                StartTime = DateTime.Now,
                Tag = 0,
                Type = Net.SocketType.Listen,
                EndPoint = endPoint
            };
            _sockets[socket] = item;
        }

        /// <summary>
        /// 添加连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        /// <param name="handler">处理实例</param>
        public void AddConnectEndPoint(IPEndPoint endPoint, SocketHandler handler)
        {
            LogPool.Logger.LogInformation("{0} {1}", "add_connect", endPoint.ToString());
            _connection.AddEndPoint(endPoint,handler);
        }

        /// <summary>
        /// 移除连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        public void RemoveConnectEndPoint(IPEndPoint endPoint)
        {
            if (_endPoints.TryGetValue(endPoint, out Socket socket))
            {
                LogPool.Logger.LogInformation("{0} {1} {2}", "remove_connect", endPoint.ToString(), socket.Handle);
                RemoveSocket(socket);
                _connection.RemoveEndPoint(endPoint);
            }     
        }

        /// <summary>
        /// 添加udp绑定地址
        /// </summary>
        /// <param name="endPoint">绑定地址</param>
        /// <param name="handler">处理实例</param>
        public void AddBindEndPoint(IPEndPoint endPoint, SocketHandler handler)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(endPoint);
            LogPool.Logger.LogInformation("{0} {1} {2}", "bind", socket.Handle, endPoint.ToString());
    
            _channel.AddSocket(socket, Net.SocketType.Udp, handler);

            SocketItem item = new SocketItem
            {
                Handler = handler,
                Socket = socket,
                StartTime = DateTime.Now,
                Tag = 0,
                Type = Net.SocketType.Udp,
                EndPoint = endPoint
            };

            _endPoints[endPoint] = socket;
            _sockets[item.Socket] = item;
        }

        /// <summary>
        /// 添加udp客户端套接字
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <returns>udp客户端套接字</returns>
        public Socket AddUdpSocket(SocketHandler handler)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            LogPool.Logger.LogInformation("{0} {1}", "udp", socket.Handle);
       
            _channel.AddSocket(socket, Net.SocketType.Udp, handler);

            _sockets[socket] = new SocketItem
            {
                Handler = handler,
                Socket = socket,
                StartTime = DateTime.Now,
                Tag = 0,
                Type = Net.SocketType.Udp
            };
            return socket;
        }

        /// <summary>
        /// 移除套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        private void RemoveSocket(Socket socket)
        {
            if (_sockets.TryRemove(socket, out SocketItem item))
            {
                //删除订阅
                if (_subscribes.Count != 0)
                {
                    AutoLock.Lock(this, () =>
                    {
                        LinkedListNode<Subscribe_Request> node = _subscribes.First;
                        while (node != null)
                        {
                            if (node.Value.Socket == socket)
                            {
                                LinkedListNode<Subscribe_Request> temp = node;
                                node = node.Next;
                                _subscribes.Remove(temp);
                            }
                            else
                            {
                                node = node.Next;
                            }
                        }
                    });
                }

                //删除字典
                if (item.Type == Net.SocketType.Accept)
                {
                    _tags.TryRemove(item.Tag, out Socket s1);
                }
                else if (item.Type == Net.SocketType.Connect ||
                         item.Type == Net.SocketType.Bind)
                {
                    _endPoints.TryRemove(item.EndPoint, out Socket s2);
                }

                //开启重连
                if (item.Type == Net.SocketType.Connect)
                {
                    _connection.ReportError();
                }

                LogPool.Logger.LogInformation(
                    "{0} {1} {2} {3} {4} {5} {6} {7} {8}",
                    "remove_socket",
                    socket.Handle,
                    socket.LocalEndPoint?.ToString(),
                    socket.RemoteEndPoint?.ToString(),
                    item.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    item.Tag,
                    item.Type,
                    item.Handler.TransmitSize,
                    item.Handler.ReceiveSize);

                //移除套接字
                _channel.RemoveSocket(socket);
            }
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(Socket socket, byte[] buffer, ReceiveAsyncHandler handler = null)
        {
            return _sockets.TryGetValue(socket, out SocketItem item) 
                ? item.Handler.SendTcp(socket, buffer, handler) 
                : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="tag">连入套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(ushort tag, byte[] buffer, ReceiveAsyncHandler handler = null)
        {
            return _tags.TryGetValue(tag, out Socket socket) 
                ? SendTcp(socket, buffer, handler) 
                : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="endPoint">发送地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(IPEndPoint endPoint, byte[] buffer,ReceiveAsyncHandler handler=null)
        {
            return _endPoints.TryGetValue(endPoint, out Socket socket)
                ? SendTcp(socket, buffer, handler)
                : SocketResult.NotFoundSocket;
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
        public SocketResult SendTcp(IPEndPoint endPoint, byte[] buffer, int protocolId,long timeStamp=0,List<byte> receiveBuffer=null)
        {
            NoticeHandler handler=new NoticeHandler(protocolId,timeStamp);
            SocketResult result= SendTcp(endPoint, buffer, handler);
            if (result == SocketResult.Success)
            {
                result= handler.Wait(AppConfig.LockTimeout) ? SocketResult.Success : SocketResult.Timeout;
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
        public SocketResult SendUdp(Socket udpSocket,IPEndPoint remoteEndPoint, byte[] buffer, ReceiveAsyncHandler handler = null)
        {
            return _sockets.TryGetValue(udpSocket, out SocketItem item)
                ? item.Handler.SendUdp(udpSocket, remoteEndPoint, buffer, handler)
                : SocketResult.NotFoundSocket;
        }

        /// <summary>
        /// udp发送
        /// </summary>
        /// <param name="bindEndPoint">本地绑定地址</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例，默认为null即不等待响应</param>
        /// <returns>发送结果</returns>
        public SocketResult SendUdp(IPEndPoint bindEndPoint, IPEndPoint remoteEndPoint, byte[] buffer, ReceiveAsyncHandler handler = null)
        {
            return _endPoints.TryGetValue(bindEndPoint, out Socket socket)
                ? SendUdp(socket, remoteEndPoint, buffer, handler)
                : SocketResult.NotFoundSocket;
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
        public SocketResult SendUdp(Socket udpSocket, IPEndPoint remoteEndPoint, byte[] buffer, int protocolId, long timeStamp=0, List<byte> receiveBuffer=null)
        {
            NoticeHandler handler = new NoticeHandler(protocolId, timeStamp);
            SocketResult result = SendUdp(udpSocket, remoteEndPoint, buffer, handler);
            if (result == SocketResult.Success)
            {
                result = handler.Wait(AppConfig.LockTimeout) ? SocketResult.Success : SocketResult.Timeout;
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
        /// 接收到协议事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void GotProtocolEventHandler(object sender,GotProtocolEventArgs e)
        {
            if (e.ProtocolId == Convert.ToByte(CollectStatus_Request.Id))
            {
                CollectStatus(e);
            }
            else if (e.ProtocolId == Convert.ToByte(Shoot_Request.Id))
            {
                Shoot(e);
            }
            else if (e.ProtocolId == Convert.ToByte(Subscribe_Request.Id))
            {
                Subscribe(e);
            }
        }

        /// <summary>
        /// 收集状态
        /// </summary>
        /// <param name="e">收到协议事件参数</param>
        private void CollectStatus(GotProtocolEventArgs e)
        {
            CollectStatus_Response cs = CollectStatus();
            e.ResponseBuffer = ProtocolPacker.Response(CollectStatus_Response.Id, e.TimeStamp, cs);
        }

        /// <summary>
        /// 收集当前服务的状态
        /// </summary>
        /// <returns>服务状态</returns>
        private CollectStatus_Response CollectStatus()
        {
            CollectStatus_Response cs = new CollectStatus_Response
            {
                SocketInfo = new List<SocketStatus>(),
                ThreadInfo = new List<ThreadStatus>()
            };
            foreach (var socket in _sockets)
            {
                SocketStatus status = new SocketStatus
                {
                    Tag = socket.Value.Tag,
                    Transmit = socket.Value.Handler.TransmitSize,
                    Receive = socket.Value.Handler.ReceiveSize,
                    Socket = socket.Key.Handle.ToInt32()
                };
                try
                {
                    if (socket.Value.Socket.RemoteEndPoint != null)
                    {
                        status.RemoteIp = BitConverter.ToUInt32(((IPEndPoint)socket.Value.Socket.RemoteEndPoint).Address.GetAddressBytes(), 0);
                        status.RemotePort = Convert.ToUInt16(((IPEndPoint)socket.Value.Socket.RemoteEndPoint).Port);
                    }
                }
                catch (SocketException)
                {

                }

                if (socket.Value.Socket.LocalEndPoint != null)
                {
                    status.LocalIp = BitConverter.ToUInt32(((IPEndPoint)socket.Value.Socket.LocalEndPoint).Address.GetAddressBytes(), 0);
                    status.LocalPort = Convert.ToUInt16(((IPEndPoint)socket.Value.Socket.LocalEndPoint).Port);
                }
                cs.SocketInfo.Add(status);
            }

            foreach (var task in _tasks)
            {
                ThreadStatus status = new ThreadStatus
                {
                    Name = task.Name,
                    TimeStamp = TimeStampConvert.ToTimeStamp(task.HitPoint),
                    Time = task.HitPoint
                };
                cs.ThreadInfo.Add(status);
            }

            return cs;
        }

        /// <summary>
        /// 转发协议
        /// </summary>
        /// <param name="e">收到协议事件参数</param>
        private void Shoot(GotProtocolEventArgs e)
        {
            Shoot_Request request = new Shoot_Request();
            ByteFormatter.Deserialize(request, e.Buffer, e.Offset);
            AutoHandler handler = new AutoHandler(request.ProtocolId, e.TimeStamp, e.Socket);
            SocketResult result;
            if (request.BindIp == 0 && request.BindPort == 0)
            {
                result = request.RemoteIp == 0 ?
                    SendTcp(request.RemotePort, request.Buffer, handler) :
                    SendTcp(new IPEndPoint(request.RemoteIp, request.RemotePort), request.Buffer, handler);
            }
            else
            {
                result = SendUdp(new IPEndPoint(request.BindIp, request.BindPort), new IPEndPoint(request.RemoteIp, request.RemotePort), request.Buffer, handler);
            }

            if (result != SocketResult.Success)
            {
                Shoot_Response response = new Shoot_Response
                {
                    Result = Convert.ToByte(result),
                    Buffer = new List<byte>()
                };
                e.ResponseBuffer = ProtocolPacker.Response(Shoot_Response.Id, e.TimeStamp, response);
            }
        }

        /// <summary>
        /// 订阅协议
        /// </summary>
        /// <param name="e">收到协议事件参数</param>
        private void Subscribe(GotProtocolEventArgs e)
        {
            Subscribe_Request request = new Subscribe_Request();
            ByteFormatter.Deserialize(request, e.Buffer, e.Offset);
            AutoLock.Lock(this, () =>
            {
                Subscribe_Request temp = _subscribes.FirstOrDefault(s =>
                    s.ProtocolId == request.ProtocolId &&
                    s.Socket == request.Socket);
                if (temp == null)
                {
                    _subscribes.AddLast(request);
                }
            });
            e.ResponseBuffer = ProtocolPacker.Response(Subscribe_Response.Id, e.TimeStamp);
        }

        /// <summary>
        /// 通知协议
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        /// <param name="buffer">字节流</param>
        protected void Notice(int protocolId, byte[] buffer)
        {
            IEnumerable<Socket> sockets = AutoLock.Lock(this, () =>
            {
                return _subscribes.Where(s =>
                    s.ProtocolId == protocolId).Select(s => s.Socket);
            });

            foreach (Socket socket in sockets)
            {
                SendTcp(socket, buffer);
            }
        }

        /// <summary>
        /// 连接到服务事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectedEventHandler(object sender, ConnectedEventArgs e)
        {
            LogPool.Logger.LogInformation("{0} {1} {2} {3}", "connected", e.Socket.Handle, e.Socket.RemoteEndPoint, e.Socket.LocalEndPoint);

            if (e.Handler == null)
            {
                LogPool.Logger.LogWarning("handler is null", e.Socket.ToString());
            }
            else
            {
                _channel.AddSocket(e.Socket, Net.SocketType.Connect, e.Handler);
                SocketItem item = new SocketItem
                {
                    Handler = e.Handler,
                    Socket = e.Socket,
                    StartTime = DateTime.Now,
                    Tag = 0,
                    Type = Net.SocketType.Connect,
                    EndPoint = (IPEndPoint)e.Socket.RemoteEndPoint
                };
                _endPoints[item.EndPoint] =e.Socket;
                _sockets[item.Socket] = item;
            }
        }

        /// <summary>
        /// 客户端连入事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptedEventHandler(object sender, AcceptedEventArgs e)
        {
            LogPool.Logger.LogInformation("{0} {1} {2} {3}", "accepted", e.AcceptSocket.Handle, e.AcceptSocket.RemoteEndPoint, e.ListenSocket.LocalEndPoint);

            if (e.Handler == null)
            {
                LogPool.Logger.LogWarning("handler is null", e.AcceptSocket.ToString());
            }
            else
            {
                _channel.AddSocket(e.AcceptSocket, Net.SocketType.Accept, e.Handler);
                _sockets[e.AcceptSocket] = new SocketItem
                {
                    Handler = e.Handler,
                    Socket = e.AcceptSocket,
                    StartTime = DateTime.Now,
                    Tag = 0,
                    Type = Net.SocketType.Accept
                };
            }
        }

        /// <summary>
        /// 套接字关闭事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClosedEventHandler(object sender, ClosedEventArgs e)
        {
            LogPool.Logger.LogInformation("{0} {1} {2}", "closed", e.Socket.Handle, e.Type);
            RemoveSocket(e.Socket);
        }

        /// <summary>
        /// 供子类实现的线程开始函数
        /// </summary>
        protected virtual void InitCore()
        {
          
        }

        /// <summary>
        /// 供子类实现的线程轮询函数
        /// </summary>
        protected virtual void PollCore()
        {

        }

        /// <summary>
        /// 供子类实现的线程结束轮询函数
        /// </summary>
        protected virtual void ExitCore()
        {

        }

        protected override void ActionCore()
        {
            InitCore();
            _channel.Start();
            _connection.Start();
            _pollIndex = 0;
            while (!IsCancelled())
            {
                PollCore();
                if (_pollIndex % 60 == 0)
                {
                    CollectStatus_Response cs = CollectStatus();
                    StringBuilder builder=new StringBuilder();
                    builder.Append("socket:\n");
                    foreach (SocketStatus status in cs.SocketInfo)
                    {
                        builder.AppendFormat("{0} local:{1} remote:{2} tag:{3} t:{4} r:{5}\n",
                            status.Socket,
                            new IPEndPoint(status.LocalIp, status.LocalPort),
                            new IPEndPoint(status.RemoteIp, status.RemotePort),
                            status.Tag,
                            status.Transmit,
                            status.Receive);
                    }

                    builder.Append("thread:\n");
                    foreach (ThreadStatus status in cs.ThreadInfo)
                    {
                        builder.AppendFormat("{0} {1}\n", status.Name, status.Time.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    }
                    _logger.LogInformation(builder.ToString());
                }
                Thread.Sleep(1000);
            }
            _connection.Stop();
            _channel.Stop();
            ExitCore();
        }
    }
}
