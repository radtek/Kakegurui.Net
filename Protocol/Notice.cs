using System;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 通知字节流协议
    /// </summary>
    public class Notice:Protocol
    {
        public override ushort Id => Convert.ToUInt16(ProtocolId.Notice);

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
        /// 字节流
        /// </summary>
        [SerializeIndex(3)]
        public byte[] Buffer { get; set; }

    }
}
