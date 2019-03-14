using System;
using System.Net;
using System.Net.Sockets;

namespace Kakegurui.Net
{
    /// <summary>
    /// 套接字发送结果
    /// </summary>
    public enum SocketResult
    {
        Success = 0,
        SendFailed = 1,
        Timeout = 2,
        Disconnection = 3,
        NotFoundSocket = 4,
        InvalidTag = 5,
        NotFoundHandler = 6,
        NotFoundEndPoint=7
    }

    /// <summary>
    /// 套接字类型
    /// </summary>
    public enum SocketType
    {
        None,
        //Tcp监听
        Listen,
        //Tcp客户端
        Accept,
        //Tcp服务端
        Connect,
        //Udp客户端
        Udp,
        //Udp服务端
        Bind
    }

    /// <summary>
    /// 套接字信息
    /// </summary>
    public class SocketItem
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 套接字处理实例
        /// </summary>
        public SocketHandler Handler { get; set; }

        /// <summary>
        /// 套接字类型
        /// </summary>
        public SocketType Type { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 套接字标识 accept
        /// </summary>
        public ushort Tag { get; set; }

        /// <summary>
        /// 套接字远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 套接字本地地址
        /// </summary>
        public IPEndPoint LocalEndPoint { get; set; }
    };
}
