﻿using System;
using System.Collections.Generic;
using Kakegurui.Core;

namespace Kakegurui.Protocol
{
    public enum ProtocolId:ushort
    {
        CollectStatus=0x0001,
        Shoot=0x0003,
        Notice=0x0005
    }

    /// <summary>
    /// 协议头
    /// </summary>
    public class ProtocolHead
    {
        /// <summary>
        /// 协议头长度
        /// </summary>
        public const byte HeadSize = 13;

        /// <summary>
        /// 协议头识别码
        /// </summary>
        [SerializeIndex(1)]
        public static byte Tag => 0xAB;

        /// <summary>
        /// 协议编号
        /// </summary>
        [SerializeIndex(2)]
        public virtual ushort Id { get; set; }

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
    /// 协议基类
    /// </summary>
    public abstract class Protocol
    {
        /// <summary>
        /// 协议编号
        /// </summary>
        public abstract ushort Id { get; }

        /// <summary>
        /// 封包请求协议
        /// </summary>
        /// <param name="protocol">协议</param>
        /// <returns>第一个字段表示请求字节流，第二个字段表示时间戳</returns>
        public static Tuple<List<byte>, long> Request(Protocol protocol)
        {
            List<byte> content = ByteFormatter.Serialize(protocol);

            ProtocolHead head = new ProtocolHead()
            {
                Id = protocol.Id,
                ContentSize = Convert.ToUInt16(content.Count),
                TimeStamp = TimeStampConvert.ToUtcTimeStamp()
            };
            List<byte> buffer = ByteFormatter.Serialize(head);
            buffer.AddRange(content);
            return new Tuple<List<byte>, long>(buffer, head.TimeStamp);
        }

        /// <summary>
        /// 封包响应协议
        /// </summary>
        /// <param name="timeStamp">发送时间戳</param>
        /// <param name="protocol">响应协议</param>
        /// <returns>响应字节流</returns>
        public static List<byte> Response(long timeStamp, Protocol protocol)
        {
            List<byte> content = ByteFormatter.Serialize(protocol);

            ProtocolHead head = new ProtocolHead()
            {
                Id = protocol.Id,
                ContentSize = Convert.ToUInt16(content.Count),
                TimeStamp = timeStamp
            };
            List<byte> buffer = ByteFormatter.Serialize(head);
            buffer.AddRange(content);
            return buffer;
        }
    }
}
