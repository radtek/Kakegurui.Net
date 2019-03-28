using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Kakegurui.Net
{
    /// <summary>
    /// 分析结果
    /// </summary>
    public enum AnalysisResult
    {
        /// <summary>
        /// 未分析出协议
        /// </summary>
        Empty,
        /// <summary>
        /// 半包
        /// </summary>
        Half,
        /// <summary>
        /// 全包
        /// </summary>
        Full
    };

    /// <summary>
    /// 接收到协议事件参数
    /// </summary>
    public class SocketPack
    {
        /// <summary>
        /// 分析结果
        /// </summary>
        public AnalysisResult Result { get; set; }

        /// <summary>
        /// 发送字节流偏移
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 发送字节流长度
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// 发送协议编号
        /// </summary>
        public ushort ProtocolId { get; set; }

        /// <summary>
        /// 发送时间戳
        /// </summary>
        public long TimeStamp { get; set; }
    }

    /// <summary>
    /// 套接字处理类
    /// </summary>
    public interface ISocketHandler
    {
        /// <summary>
        /// 拆包
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">远程地址，udp有效</param>
        /// <param name="buffer">字节流</param>
        /// <param name="offset">偏移量</param>
        /// <returns>处理结果</returns>
        SocketPack Unpack(Socket socket,IPEndPoint remoteEndPoint,List<byte> buffer,int offset);

    };
}
