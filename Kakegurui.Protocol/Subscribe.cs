
using System.Net.Sockets;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 请求订阅协议
    /// </summary>
    public class Subscribe_Request
    {
        public static byte Id => 0x05;

        /// <summary>
        /// 协议编号
        /// </summary>
        [SerializeIndex(1)]
        public int ProtocolId { get; set; }

        public Socket Socket { get; set; }
    }

    /// <summary>
    /// 响应订阅协议
    /// </summary>
    public class Subscribe_Response
    {
        public static byte Id => 0x06;
    }
}
