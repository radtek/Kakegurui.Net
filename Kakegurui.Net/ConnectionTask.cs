using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Kakegurui.Core;

namespace Kakegurui.Net
{
    /// <summary>
    /// 连接到服务事件参数
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 套接字远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 套接字本地地址
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }

        /// <summary>
        /// 套接字处理实例
        /// </summary>
        public SocketHandler Handler { get; set; }
    }

    /// <summary>
    /// 连接服务线程
    /// </summary>
    public class ConnectionTask:TaskObject
    {
        /// <summary>
        /// 连接间隔时间
        /// </summary>
        private const int ConnectionSpan = 5000;
        /// <summary>
        /// 连接地址集合
        /// </summary>
        private readonly ConcurrentDictionary<IPEndPoint, SocketItem> _endPoints = new ConcurrentDictionary<IPEndPoint, SocketItem>();

        /// <summary>
        /// 条件变量
        /// </summary>
        private readonly AutoResetEvent _eventWait = new AutoResetEvent(false);

        /// <summary>
        /// 连接到服务事件
        /// </summary>
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConnectionTask() 
            : base("connection")
        {
        }

        /// <summary>
        /// 添加连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        /// <param name="handler">执行实例</param>
        public void AddEndPoint(IPEndPoint endPoint,SocketHandler handler)
        {
            _endPoints[endPoint] = new SocketItem
            {
                Handler = handler
            };
            _eventWait.Set();
        }

        /// <summary>
        /// 移除连接地址
        /// </summary>
        /// <param name="endPoint">连接地址</param>
        public void RemoveEndPoint(IPEndPoint endPoint)
        {
            if (_endPoints.TryRemove(endPoint, out SocketItem item))
            {
                item.Socket = null;
            }
            _eventWait.Set();
        }

        /// <summary>
        /// 报告连接套接字错误
        /// </summary>
        public void ReportError(IPEndPoint endPoint)
        {
            if (_endPoints.TryGetValue(endPoint, out SocketItem item))
            {
                item.Socket = null;
            }
            _eventWait.Set();
        }

        public override void Stop()
        {
            _eventWait.Set();
            base.Stop();
        }

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                foreach (var pair in _endPoints)
                {
                    if (pair.Value.Socket?.Connected!=true)
                    {
                        Socket socket = new Socket(
                            AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            socket.Connect(pair.Key);
                            Connected?.Invoke(this,new ConnectedEventArgs
                            {
                                Socket = socket,
                                RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint,
                                LocalEndPoint = (IPEndPoint)socket.LocalEndPoint,
                                Handler = pair.Value.Handler.Clone()
                            });
                            pair.Value.Socket = socket;
                        }
                        catch (SocketException)
                        {
                            socket.Close();
                        }
                    }
                }

                if (_endPoints.Count==0||_endPoints.All(e => e.Value.Socket?.Connected==true))
                {
                    _eventWait.WaitOne();
                }
                else
                {
                    Thread.Sleep(ConnectionSpan);
                }
            }
        }
    }
}
