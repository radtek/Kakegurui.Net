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
        /// 套接字集合
        /// </summary>
        protected readonly ConcurrentDictionary<Socket,SocketChannel> _sockets=new ConcurrentDictionary<Socket, SocketChannel>();

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

        }

        /// <summary>
        /// 客户端连入事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void AcceptedEventHandler(object sender, AcceptedEventArgs e)
        {
            e.Channel.Closed += ClosedEventHandler;
            _sockets[e.Socket] = e.Channel;
        }

        /// <summary>
        /// 连接到服务事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConnectedEventHandler(object sender, ConnectedEventArgs e)
        {
            ((SocketChannel) sender).Closed += ClosedEventHandler;
            _sockets[e.Socket] = (SocketChannel) sender;
        }

        /// <summary>
        /// 套接字关闭事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ClosedEventHandler(object sender, ClosedEventArgs e)
        {
            _sockets.TryRemove(e.Socket, out SocketChannel channel);
        }

        /// <summary>
        /// 添加tcp监听端口
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">监听端口</param>
        public SocketChannel AddListenEndPoint(ISocketHandler handler, int port)
        {
            SocketChannel channel = new SocketChannel(SocketType.Listen, new IPEndPoint(IPAddress.Any, port), new IPEndPoint(0, 0), null,handler);
            if (channel.Socket != null)
            {
                channel.Accepted += AcceptedEventHandler;
                _sockets[channel.Socket] = channel;
            }

            return channel;
        }

        /// <summary>
        /// 添加连接地址
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="endPoint">连接地址</param>
        public SocketChannel AddConnectEndPoint(ISocketHandler handler, IPEndPoint endPoint)
        {
            SocketChannel channel = new SocketChannel(SocketType.Connect, null, endPoint, endPoint.ToString(), handler);
            channel.Connected += ConnectedEventHandler;
            return channel;
        }

        /// <summary>
        /// 添加udp绑定端口
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">绑定端口，默认为0，表示绑定任意端口，此时表示udp客户端</param>
        /// <returns>udp客户端套接字</returns>
        public SocketChannel AddBindEndPoint(ISocketHandler handler, int port=0)
        {
            IPEndPoint localEndPoint= new IPEndPoint(IPAddress.Any, port);
            SocketChannel channel= new SocketChannel(SocketType.Bind, localEndPoint, new IPEndPoint(0, 0), localEndPoint.ToString(), handler);
            if (channel.Socket != null)
            {
                _sockets[channel.Socket] = channel;
            }
            return channel;
        }

        /// <summary>
        /// 主动移除套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        protected void RemoveSocket(Socket socket)
        {
            if (_sockets.TryRemove(socket, out SocketChannel channel))
            {
                channel.Close();
            }
        }

        /// <summary>
        /// 套接字
        /// </summary>
        /// <param name="handler">套接字详情</param>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="action">成功响应后的异步回调，默认为null，表示同步等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        private SocketResult Send(SocketChannel handler, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match = null, Action<ReceivedEventArgs> action=null,List<byte> receiveBuffer = null,int timeout = 3000)
        {
            return handler.Send(remoteEndPoint, buffer,match, action, receiveBuffer, timeout);
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
        public SocketResult SendTcp(Socket socket, List<byte> buffer, Func<ReceivedEventArgs, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketChannel handler) ? Send(handler,null, buffer, match, null,receiveBuffer, timeout) : SocketResult.NotFoundSocket;
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
        public SocketResult SendTcpAsync(Socket socket, List<byte> buffer, Func<ReceivedEventArgs, bool> match, Action<ReceivedEventArgs> action, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketChannel handler) ? Send(handler, null, buffer, match, action,null, timeout) : SocketResult.NotFoundSocket;
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
        public SocketResult SendTcp(string tag, List<byte> buffer,Func<ReceivedEventArgs,bool> match=null, List<byte> receiveBuffer = null, int timeout = 3000)
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
        public SocketResult SendTcpAsync(string tag, List<byte> buffer, Func<ReceivedEventArgs, bool> match , Action<ReceivedEventArgs> action, int timeout = 3000)
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
        public SocketResult SendUdp(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketChannel handler) ? Send(handler, remoteEndPoint, buffer, match, null, receiveBuffer, timeout) : SocketResult.NotFoundSocket;
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
        public SocketResult SendUdpAsync(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match, Action<ReceivedEventArgs> action, int timeout = 3000)
        {
            return _sockets.TryGetValue(socket, out SocketChannel handler) ? Send(handler, remoteEndPoint, buffer, match, action, null, timeout) : SocketResult.NotFoundSocket;
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
        public SocketResult SendUdp(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match = null, List<byte> receiveBuffer = null, int timeout = 3000)
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
        public SocketResult SendUdpAsync(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match, Action<ReceivedEventArgs> action, int timeout = 3000)
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
                                socket.Value.TransmitSize,
                                socket.Value.ReceiveSize, socket.Value.StartTime);
                        }
                        catch (SocketException)
                        {
                            builder.AppendFormat("{0} local:{1} tag:{2} t:{3} r:{4}\n",
                                socket.Key.Handle,
                                socket.Value.LocalEndPoint,
                                socket.Value.Tag,
                                socket.Value.TransmitSize,
                                socket.Value.ReceiveSize);
                        }
                    }
                    LogPool.Logger.LogTrace(builder.ToString());
                }
                ++monitorPoll;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            foreach (var pair in _sockets)
            {
                pair.Value.Close();
            }
        }

    }
}
