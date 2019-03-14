using System.Collections.Generic;
using System.Threading;
using Kakegurui.Core;

namespace Kakegurui.Net
{

    /// <summary>
    /// 接收到数据后执行通知处理
    /// </summary>
    public class NoticeHandler : ReceiveAsyncHandler
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
            : this(protocolId, 0)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        /// <param name="timeStamp">时间戳</param>
        public NoticeHandler(int protocolId, long timeStamp)
            : base(protocolId, timeStamp)
        {
            _isNoticed = false;
        }

        /// <summary>
        /// 等待响应数据
        /// </summary>
        /// <returns>返回true表示收到回复，返回false表示超时</returns>
        public bool Wait()
        {
            bool result = _event.WaitOne(AppConfig.LockTimeout);
            _isNoticed = true;
            return result;
        }

        public override bool IsCompleted()
        {
            return _isNoticed;
        }

        public override void Handle(List<byte> buffer, int offset, int size)
        {
            Buffer = new List<byte>(buffer.GetRange(offset, size));
            _event.Set();
            _isNoticed = true;
        }
    }
}
