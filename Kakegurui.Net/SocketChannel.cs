using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Kakegurui.Core;

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
    /// 套接字通道
    /// </summary>
    public class SocketChannel:TaskObject
    {
        /// <summary>
        /// 客户端连入事件
        /// </summary>
        public event EventHandler<AcceptedEventArgs> Accepted;

        /// <summary>
        /// 关闭套接字事件
        /// </summary>
        public event EventHandler<ClosedEventArgs> Closed;
     
        /// <summary>
        /// 套接字集合
        /// </summary>
        private readonly ConcurrentDictionary<Socket,SocketItem> _sockets=new ConcurrentDictionary<Socket, SocketItem>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public SocketChannel()
            :base("socket channel")
        {

        }

        /// <summary>
        /// 添加套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="type">套接字类型</param>
        /// <param name="handler">处理函数</param>
        public void AddSocket(Socket socket,SocketType type,SocketHandler handler)
        {
            SocketItem item= new SocketItem
            {
                Socket = socket,
                Type = type,
                Handler = handler
            };
            _sockets[socket] = item;
            if (item.Type == SocketType.Listen)
            {
                AcceptAsync(item);
            }
            else
            {
                if (item.Type == SocketType.Udp)
                {
                    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.Completed += ReceivedHandler;
                    receiveArgs.SetBuffer(new byte[65536], 0, 65536);
                    receiveArgs.RemoteEndPoint = item.Socket.LocalEndPoint;
                    receiveArgs.UserToken = item;
                    item.Socket.ReceiveFromAsync(receiveArgs);
                }
                else
                {
                    SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                    receiveArgs.Completed += ReceivedHandler;
                    receiveArgs.SetBuffer(new byte[65536], 0, 65536);
                    receiveArgs.AcceptSocket = item.Socket;
                    receiveArgs.UserToken = item;
                    item.Socket.ReceiveAsync(receiveArgs);
                }
            }
        }

        /// <summary>
        /// 移除套接字
        /// </summary>
        /// <param name="socket">套接字</param>
        public void RemoveSocket(Socket socket)
        {
            if (_sockets.TryRemove(socket, out SocketItem item))
            {
                item.Socket.Close();
            }
        }

        /// <summary>
        /// 开始异步等待客户端套接字
        /// </summary>
        /// <param name="item">套接字信息</param>
        private void AcceptAsync(SocketItem item)
        {
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += AcceptedEventHandler;
            acceptArgs.UserToken = item;
            item.Socket.AcceptAsync(acceptArgs);
        }

        /// <summary>
        /// 等待套接字事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptedEventHandler(object sender, SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket.RemoteEndPoint == null)
            {
                return;
            }
            Accepted?.Invoke(this,new AcceptedEventArgs
            {
                ListenSocket = (Socket)sender,
                AcceptSocket = e.AcceptSocket,
                Handler = ((SocketItem)e.UserToken).Handler.Clone()
            });
            AcceptAsync((SocketItem)e.UserToken);
        }

        /// <summary>
        /// 接收事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceivedHandler(object sender, SocketAsyncEventArgs e)
        {
            Socket socket = (Socket) sender;
            SocketItem item = (SocketItem) e.UserToken;
            if (e.BytesTransferred == 0)
            {
                Closed?.Invoke(this,new ClosedEventArgs
                {
                    Socket = item.Socket,
                    Type = item.Type
                });
            }
            else
            {
                if (item.Type == SocketType.Udp)
                {
                    item.Handler?.Handle(socket, e.Buffer, e.BytesTransferred,(IPEndPoint)e.RemoteEndPoint);
                    socket.ReceiveFromAsync(e);
                }
                else
                {
                    item.Handler?.Handle(socket, e.Buffer, e.BytesTransferred);
                    socket.ReceiveAsync(e);
                }
            }
        }

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {           
                Thread.Sleep(AppConfig.LongSleepSpan);
            }
        }

    }
}
