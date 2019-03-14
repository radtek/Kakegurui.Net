using System.Collections.Generic;

namespace Kakegurui.Net
{
    /// <summary>
    /// 接收数据执行接口
    /// </summary>
    public abstract class ReceiveAsyncHandler
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        /// <param name="timeStamp">时间戳</param>
        protected ReceiveAsyncHandler(int protocolId, long timeStamp)
        {
            ProtocolId = protocolId;
            TimeStamp = timeStamp;
        }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long TimeStamp { get; }

        /// <summary>
        /// 协议编号
        /// </summary>
        public int ProtocolId { get; }

        /// <summary>
        /// 是否处理完成
        /// </summary>
        /// <returns>处理完成返回true，否则返回false</returns>
        public abstract bool IsCompleted();

        /// <summary>
        /// 处理接收数据
        /// </summary>
        /// <param name="buffer">字节流</param>
        /// <param name="offset">偏移量</param>
        /// <param name="size">字节流长度</param>
        public abstract void Handle(List<byte> buffer, int offset, int size);

    }
}
