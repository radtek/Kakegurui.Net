using System;
using System.Collections.Generic;
using System.Net;
using Kakegurui.Core;
using Kakegurui.Net;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 转发协议
    /// </summary>
    public class Shoot_Request
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public Shoot_Request()
        {

        }

        /// <summary>
        /// 构造函数 tcp
        /// </summary>
        /// <param name="tag">连入套接字标记</param>
        /// <param name="protocol">等待协议编号</param>
        /// <param name="buffer">字节流</param>
        public Shoot_Request(string tag, short protocol, List<byte> buffer)
        {
            Tag = tag;
            RemoteIp = 0;
            RemotePort = 0;
            ProtocolId = protocol;
            Buffer = buffer;
        }

        /// <summary>
        /// 构造函数 udp
        /// </summary>
        /// <param name="tag">连入套接字标记</param>
        /// <param name="remoteEndPoint">udp远程地址</param>
        /// <param name="protocol">等待协议编号</param>
        /// <param name="buffer">字节流</param>
        public Shoot_Request(string tag,IPEndPoint remoteEndPoint, short protocol, List<byte> buffer)
        {
            Tag = tag;
            RemoteIp = BitConverter.ToUInt32(remoteEndPoint.Address.GetAddressBytes(),0);
            RemotePort = Convert.ToUInt16(remoteEndPoint.Port);
            ProtocolId = protocol;
            Buffer = buffer;
        }

        public static byte Id => 0x03;

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
        [SerializeIndex(4)] public short ProtocolId { get; set; }

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
        public override byte Id => 0x04;

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
