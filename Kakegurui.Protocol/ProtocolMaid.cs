using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Kakegurui.Core;
using Kakegurui.Net;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 协议收发
    /// </summary>
    public class ProtocolMaid:SocketMaid
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ProtocolMaid()
            : this("protocol maid")
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">线程名</param>
        public ProtocolMaid(string name)
            :base(name)
        {

        }

        /// <summary>
        /// 收集状态
        /// </summary>
        /// <param name="e">收到协议事件参数</param>
        protected void CollectStatus(GotProtocolEventArgs e)
        {
            CollectStatus_Response cs = new CollectStatus_Response
            {
                SocketInfo = new List<SocketStatus>(),
                ThreadInfo = new List<ThreadStatus>()
            };
            foreach (var socket in _sockets)
            {
                SocketStatus status = new SocketStatus
                {
                    Tag = socket.Value.Tag,
                    Transmit = socket.Value.Handler.TransmitSize,
                    Receive = socket.Value.Handler.ReceiveSize,
                    Socket = socket.Key.Handle.ToInt32()
                };
                try
                {
                    if (socket.Value.Socket.RemoteEndPoint != null)
                    {
                        status.RemoteIp = BitConverter.ToUInt32(((IPEndPoint)socket.Value.Socket.RemoteEndPoint).Address.GetAddressBytes(), 0);
                        status.RemotePort = Convert.ToUInt16(((IPEndPoint)socket.Value.Socket.RemoteEndPoint).Port);
                    }
                }
                catch (SocketException)
                {

                }

                if (socket.Value.Socket.LocalEndPoint != null)
                {
                    status.LocalIp = BitConverter.ToUInt32(((IPEndPoint)socket.Value.Socket.LocalEndPoint).Address.GetAddressBytes(), 0);
                    status.LocalPort = Convert.ToUInt16(((IPEndPoint)socket.Value.Socket.LocalEndPoint).Port);
                }
                cs.SocketInfo.Add(status);
            }

            foreach (var task in _tasks)
            {
                ThreadStatus status = new ThreadStatus
                {
                    Name = task.Name,
                    TimeStamp = TimeStampConvert.ToTimeStamp(task.HitPoint),
                    Time = task.HitPoint
                };
                cs.ThreadInfo.Add(status);
            }
            e.ResponseBuffer = ProtocolPacker.Response(CollectStatus_Response.Id, e.TimeStamp, cs);
        }

        /// <summary>
        /// 转发协议
        /// </summary>
        /// <param name="sender">协议处理实例</param>
        /// <param name="e">收到协议事件参数</param>
        protected void Shoot(object sender,GotProtocolEventArgs e)
        {
            Shoot_Request request = new Shoot_Request();
            ByteFormatter.Deserialize(request, e.Buffer, e.Offset+ProtocolHead.HeadSize);
            AutoHandler handler = new AutoHandler(request.ProtocolId, e.TimeStamp, e.Socket,(SocketHandler)sender);
            SocketResult result;
            if (request.BindIp == 0 && request.BindPort == 0)
            {
                result = request.RemoteIp == 0 ?
                    SendTcp(request.RemotePort, request.Buffer, handler) :
                    SendTcp(new IPEndPoint(request.RemoteIp, request.RemotePort), request.Buffer, handler);
            }
            else
            {
                result = SendUdp(new IPEndPoint(request.BindIp, request.BindPort), new IPEndPoint(request.RemoteIp, request.RemotePort), request.Buffer, handler);
            }

            if (result != SocketResult.Success)
            {
                Shoot_Response response = new Shoot_Response
                {
                    Result = Convert.ToByte(result),
                    Buffer = new List<byte>()
                };
                e.ResponseBuffer = ProtocolPacker.Response(Shoot_Response.Id, e.TimeStamp, response);
            }
        }

    }
}
