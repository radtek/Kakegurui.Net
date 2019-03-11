using System;
using System.Collections.Generic;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 协议头
    /// </summary>
    public class ProtocolHead
    {
        /// <summary>
        /// 协议头长度
        /// </summary>
        public const byte HeadSize = 12;

        /// <summary>
        /// 协议头识别码
        /// </summary>
        [SerializeIndex(1)]
        public static byte Tag => 0xAB;

        /// <summary>
        /// 协议编号
        /// </summary>
        [SerializeIndex(2)]
        public virtual byte Id { get; set; }

        /// <summary>
        /// 协议内容长度
        /// </summary>
        [SerializeIndex(3)]
        public ushort ContentSize { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [SerializeIndex(4)]
        public long TimeStamp { get; set; }
    }

    /// <summary>
    /// 协议包装
    /// </summary>
    public class ProtocolPacker
    {
        /// <summary>
        /// 封包请求协议
        /// </summary>
        /// <param name="id">协议编号</param>
        /// <param name="data">协议数据</param>
        /// <returns>第一个字段表示请求字节流，第二个字段表示时间戳</returns>
        public static Tuple<List<byte>,long> Request(byte id, object data = null)
        {
            List<byte> content = data == null ?
                new List<byte>() :
                ByteFormatter.Serialize(data);
          
            ProtocolHead head = new ProtocolHead()
            {
                Id = id,
                ContentSize = Convert.ToUInt16(content.Count),
                TimeStamp = TimeStampConvert.ToTimeStamp()
            };
            List<byte> buffer = ByteFormatter.Serialize(head);
            buffer.AddRange(content);
            return new Tuple<List<byte>, long>(buffer, head.TimeStamp);
        }

        /// <summary>
        /// 封包响应协议
        /// </summary>
        /// <param name="id">协议编号</param>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="data">响应数据</param>
        /// <returns>响应字节流</returns>
        public static List<byte> Response(byte id,long timeStamp, object data = null)
        {
            List<byte> content = data == null ?
                new List<byte>() :
                ByteFormatter.Serialize(data);

            ProtocolHead head = new ProtocolHead()
            {
                Id = id,
                ContentSize = Convert.ToUInt16(content.Count),
                TimeStamp = timeStamp
            };
            List<byte> buffer = ByteFormatter.Serialize(head);
            buffer.AddRange(content);
            return buffer;
        }

    }

}
