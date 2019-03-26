using System;
using System.Collections.Generic;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 套接字状态
    /// </summary>
    public class SocketStatus
    {
        /// <summary>
        /// 远程地址
        /// </summary>
        [SerializeIndex(1)]
        public uint RemoteIp { get; set; }

        /// <summary>
        /// 远程端口
        /// </summary>
        [SerializeIndex(2)]
        public ushort RemotePort { get; set; }

        /// <summary>
        /// 本地地址
        /// </summary>
        [SerializeIndex(3)]
        public uint LocalIp { get; set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        [SerializeIndex(4)]
        public ushort LocalPort { get; set; }

        /// <summary>
        /// 套接字标记
        /// </summary>
        [SerializeIndex(5)]
        public string Tag { get; set; }

        /// <summary>
        /// 发送总字节流
        /// </summary>
        [SerializeIndex(6)]
        public ulong Transmit { get; set; }

        /// <summary>
        /// 接收总字节流
        /// </summary>
        [SerializeIndex(7)]
        public ulong Receive { get; set; }

    }

    /// <summary>
    /// 请求状态协议
    /// </summary>
    public class CollectStatus_Request:Protocol
    {
        public override ushort Id => Convert.ToUInt16(ProtocolId.CollectStatus);
    }

    /// <summary>
    /// 响应状态协议
    /// </summary>
    public class CollectStatus_Response:Protocol
    {
        public override ushort Id => Convert.ToUInt16(ProtocolId.CollectStatus + 1);

        /// <summary>
        /// 套接字状态集合
        /// </summary>
        [SerializeIndex(1)]
        public List<SocketStatus> SocketInfo { get; set; }
    }
}
