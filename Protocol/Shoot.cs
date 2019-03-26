using System;
using System.Collections.Generic;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 转发协议
    /// </summary>
    public class Shoot_Request : Protocol
    {
        public override ushort Id => Convert.ToUInt16(Kakegurui.Protocol.ProtocolId.Shoot);

        /// <summary>
        /// 本地地址ip
        /// </summary>
        [SerializeIndex(1)] public string Tag { get; set; }

        /// <summary>
        /// 远程地址ip
        /// </summary>
        [SerializeIndex(2)] public uint RemoteIp { get; set; }

        /// <summary>
        /// 远程地址端口
        /// </summary>
        [SerializeIndex(3)] public ushort RemotePort { get; set; }

        /// <summary>
        /// 等待协议编号
        /// </summary>
        [SerializeIndex(4)] public ushort ProtocolId { get; set; }

        /// <summary>
        /// 发送字节流
        /// </summary>
        [SerializeIndex(5)] public List<byte> Buffer { get; set; }
    }

    /// <summary>
    /// 转发响应协议
    /// </summary>
    public class Shoot_Response:Protocol
    {
        public override ushort Id => Convert.ToUInt16(ProtocolId.Shoot+1);

        /// <summary>
        /// 转发结果
        /// </summary>
        [SerializeIndex(1)] public byte Result { get; set; }

        /// <summary>
        /// 响应字节流
        /// </summary>
        [SerializeIndex(2)] public List<byte> Buffer { get; set; }
    }
}
