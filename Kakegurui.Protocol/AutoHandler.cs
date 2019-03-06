using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Kakegurui.Core;
using Kakegurui.Net;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 自动转发接收到的字节流
    /// </summary>
    public class AutoHandler : ReceiveAsyncHandler
    {
        /// <summary>
        /// 是否已经处理过
        /// </summary>
        private bool _isHandled;

        /// <summary>
        /// 开始等待的时间戳
        /// </summary>
        private readonly DateTime _initTimeStamp;

        /// <summary>
        /// 发送时需要的套接字
        /// </summary>
        private readonly long _shootTimeStamp;

        /// <summary>
        /// 发送到的套接字
        /// </summary>
        private readonly Socket _guestSocket;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="protocolId">协议编号</param>
        /// <param name="shootTimeStamp">发送时需要的时间戳</param>
        /// <param name="targetSocket">发送到的套接字</param>
        public AutoHandler(int protocolId, long shootTimeStamp, Socket targetSocket)
            : base(protocolId, 0)
        {
            _shootTimeStamp = shootTimeStamp;
            _guestSocket = targetSocket;
            _initTimeStamp = DateTime.Now;
            _isHandled = false;
        }

        public override bool IsCompleted()
        {
            return (DateTime.Now - _initTimeStamp).TotalMilliseconds > AppConfig.LockTimeout || _isHandled;
        }

        public override void Handle(List<byte> buffer, int offset, int size)
        {
            _isHandled = true;
            Shoot_Response shoot = new Shoot_Response
            {
                Result = Convert.ToByte(SocketResult.Success),
                Buffer = buffer.GetRange(offset, size)
            };
            byte[] responseBuffer=ProtocolPacker.Response(Shoot_Response.Id, _shootTimeStamp,shoot);
            int written = 0;
            while (written != responseBuffer.Length)
            {
                int n;
                if ((n = _guestSocket.Send(responseBuffer, written, responseBuffer.Length - written, SocketFlags.None)) <= 0)
                {
                    break;
                }
                written += n;
            }
        }
    }
}
