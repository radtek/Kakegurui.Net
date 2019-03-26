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
        protected readonly ConcurrentDictionary<int, SocketChannel> _sockets=new ConcurrentDictionary<int, SocketChannel>();

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
            _sockets[e.Socket.Handle.ToInt32()] = e.Channel;
        }

        /// <summary>
        /// 连接到服务事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConnectedEventHandler(object sender, ConnectedEventArgs e)
        {
            ((SocketChannel) sender).Closed += ClosedEventHandler;
            _sockets[e.Socket.Handle.ToInt32()] = (SocketChannel) sender;
        }

        /// <summary>
        /// 套接字关闭事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ClosedEventHandler(object sender, ClosedEventArgs e)
        {
            _sockets.TryRemove(e.Socket.Handle.ToInt32(), out SocketChannel channel);
        }

        /// <summary>
        /// 添加tcp监听端口
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">监听端口</param>
        public SocketChannel AddListenEndPoint(ISocketHandler handler, int port)
        {
            SocketChannel channel = new SocketChannel(null, SocketType.Listen, new IPEndPoint(IPAddress.Any, port), null,handler);
            if (channel.Socket != null)
            {
                channel.Accepted += AcceptedEventHandler;
                _sockets[channel.Socket.Handle.ToInt32()] = channel;
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
            SocketChannel channel = new SocketChannel(null, SocketType.Connect, null, endPoint, handler);
            channel.Connected += ConnectedEventHandler;
            return channel;
        }

        /// <summary>
        /// 添加udp绑定端口
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <param name="port">绑定端口</param>
        /// <returns>udp客户端套接字</returns>
        public SocketChannel AddUdpServer(ISocketHandler handler, int port)
        {
            IPEndPoint localEndPoint= new IPEndPoint(IPAddress.Any, port);
            SocketChannel channel= new SocketChannel(null,SocketType.Udp_Server, localEndPoint, null, handler);
            if (channel.Socket != null)
            {
                _sockets[channel.Socket.Handle.ToInt32()] = channel;
            }
            return channel;
        }

        /// <summary>
        /// 添加udp绑定端口
        /// </summary>
        /// <param name="handler">处理实例</param>
        /// <returns>udp客户端套接字</returns>
        public SocketChannel AddUdpClient(ISocketHandler handler)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
            SocketChannel channel = new SocketChannel(null, SocketType.Udp_Client, localEndPoint, null, handler);
            if (channel.Socket != null)
            {
                _sockets[channel.Socket.Handle.ToInt32()] = channel;
            }
            return channel;
        }

        /// <summary>
        /// 主动移除套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        protected void RemoveSocket(int socket)
        {
            if (_sockets.TryRemove(socket, out SocketChannel channel))
            {
                channel.Close();
            }
        }

        /// <summary>
        /// tcp广播
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        public IEnumerable<SocketResult> Broadcast(string tag, List<byte> buffer)
        {
            return _sockets.AsParallel()
                .Where(s => s.Value.Tag == tag)
                .Select(s => s.Value.Send(null, buffer));
        }

        /// <summary>
        /// tcp发送，同步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址,null表示tcp发送</param> 
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult Send(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match, List<byte> receiveBuffer=null, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key==0 ? SocketResult.NotFoundSocket : socket.Value.Send(remoteEndPoint, buffer, match, null, receiveBuffer, timeout);
        }

        /// <summary>
        /// tcp发送，异步响应
        /// </summary>
        /// <param name="tag">套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址,null表示tcp发送</param> 
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数</param>
        /// <param name="action">成功响应后的异步回调，表示同步等待响应</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult SendAsync(string tag, IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match, Action<ReceivedEventArgs> action, int timeout = 3000)
        {
            var socket = _sockets.FirstOrDefault(s => s.Value.Tag == tag);
            return socket.Key ==0 ? SocketResult.NotFoundSocket : socket.Value.Send(remoteEndPoint, buffer, match, action, null, timeout);
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
                        builder.Append(socket.Value);
                        builder.Append("\n");
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
