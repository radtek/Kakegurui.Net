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
    /// 连接服务线程
    /// </summary>
    public class ConnectionTask:TaskObject
    {
        /// <summary>
        /// 连接地址集合
        /// </summary>
        private readonly ConcurrentDictionary<SocketItem,object> _endPoints = new ConcurrentDictionary<SocketItem, object>();

        /// <summary>
        /// 条件变量
        /// </summary>
        private readonly AutoResetEvent _eventWait = new AutoResetEvent(false);

        /// <summary>
        /// 连接到服务事件
        /// </summary>
        public event EventHandler<SocketEventArgs> Connected;

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
        /// <param name="item">连接地址</param>
        public void AddEndPoint(SocketItem item)
        {
            _endPoints[item] = null;
            _eventWait.Set();
        }

        /// <summary>
        /// 报告连接套接字错误
        /// </summary>
        public void ReportError(SocketItem item)
        {
            item.Socket = null;
            _eventWait.Set();
        }

        public override void Stop()
        {
            _eventWait.Set();
            base.Stop();
        }

        protected override void ActionCore()
        {
            int pollIndex = 0;
            while (!IsCancelled())
            {
                if (pollIndex % 5 == 0)
                {
                    _endPoints.AsParallel().ForAll(pair =>
                    {
                        if (pair.Key.Socket?.Connected != true)
                        {
                            Socket socket = new Socket(
                                AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
                            try
                            {
                                socket.Connect(pair.Key.RemoteEndPoint);
                                pair.Key.Socket = socket;
                                pair.Key.LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
                                pair.Key.Handler = pair.Key.Handler.Clone();
                                pair.Key.StartTime = DateTime.Now;
                                Connected?.Invoke(this, new SocketEventArgs
                                {
                                    Item = pair.Key
                                });
                            }
                            catch (SocketException)
                            {
                                socket.Close();
                            }
                        }
                    });
                }

                ++pollIndex;
                if (_endPoints.Count == 0 || _endPoints.All(e => e.Key.Socket?.Connected == true))
                {
                    _eventWait.WaitOne();
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
