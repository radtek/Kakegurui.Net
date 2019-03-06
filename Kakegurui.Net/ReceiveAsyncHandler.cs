using System.Collections.Generic;
using System.Threading;

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

    /// <summary>
    /// 接收到数据后执行通知处理
    /// </summary>
    public class NoticeHandler:ReceiveAsyncHandler
    {
        /// <summary>
        /// 条件变量
        /// </summary>
        private readonly AutoResetEvent _event = new AutoResetEvent(false);

        /// <summary>
        /// 表示是否已经通知过
        /// </summary>
        private bool _isNoticed;

        /// <summary>
        /// 收到的字节流
        /// </summary>
        public List<byte> Buffer { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        public NoticeHandler(int protocolId)
            :this(protocolId,0)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        /// <param name="timeStamp">时间戳</param>
        public NoticeHandler(int protocolId, long timeStamp)
            :base(protocolId,timeStamp)
        {
            _isNoticed = false;
        }

        /// <summary>
        /// 等待响应数据
        /// </summary>
        /// <param name="milliseconds">超时时间</param>
        /// <returns></returns>
        public bool Wait(int milliseconds)
        {
            bool result=_event.WaitOne(milliseconds);
            _isNoticed = true;
            return result;
        }

        public override bool IsCompleted()
        {
            return _isNoticed;
        }

        public override void Handle(List<byte> buffer, int offset, int size)
        {
            Buffer=new List<byte>(buffer.GetRange(offset, size));
            _event.Set();
            _isNoticed = true;
        }
    }
}
