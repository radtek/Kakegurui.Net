using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
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
            : this("protocol_maid")
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">线程名</param>
        public ProtocolMaid(string name)
            :base(name)
        {
            _handler.Where(p => p.ProtocolId == Convert.ToInt32(ProtocolId.CollectStatus))
                .Subscribe(CollectStatus);
            _handler.Where(p => p.ProtocolId == Convert.ToInt32(ProtocolId.Shoot))
                .Subscribe(Shoot);
        }

        private readonly ProtocolHandler _handler = new ProtocolHandler();

        /// <summary>
        /// 添加tcp监听端口
        /// </summary>
        /// <param name="port">监听端口</param>
        public void AddListenEndPoint(int port)
        {
            AddListenEndPoint(port,_handler);
        }

        /// <summary>
        /// 添加udp绑定端口
        /// </summary>
        /// <param name="port">监听端口</param>
        public void AddBindEndPoint(int port)
        {
            AddBindEndPoint(_handler, port);
        }

        /// <summary>
        /// 收集状态
        /// </summary>
        /// <param name="pack">协议包</param>
        protected void CollectStatus(SocketPack pack)
        {
            CollectStatus_Response cs = new CollectStatus_Response
            {
                SocketInfo = new List<SocketStatus>()
            };
            foreach (var socket in _sockets)
            {
                SocketStatus status = new SocketStatus
                {
                    Tag = socket.Value.Tag,
                    Transmit = socket.Value.Handler.TransmitSize,
                    Receive = socket.Value.Handler.ReceiveSize,
                    LocalIp = BitConverter.ToUInt32(socket.Value.LocalEndPoint.Address.GetAddressBytes(), 0),
                    LocalPort = Convert.ToUInt16(socket.Value.LocalEndPoint.Port),
                    RemoteIp = BitConverter.ToUInt32(socket.Value.RemoteEndPoint.Address.GetAddressBytes(), 0),
                    RemotePort = Convert.ToUInt16(socket.Value.RemoteEndPoint.Port)
                };
                cs.SocketInfo.Add(status);
            }
            pack.Handler.Send(pack.Socket, pack.RemoteEndPoint, Protocol.Response(pack.TimeStamp, cs));
        }

        /// <summary>
        /// 转发协议
        /// </summary>
        /// <param name="pack">协议包</param>
        protected void Shoot(SocketPack pack)
        {
            Shoot_Request request = new Shoot_Request();
            ByteFormatter.Deserialize(request, pack.Buffer, pack.Offset+ProtocolHead.HeadSize);
            if (request.RemoteIp == 0 && request.RemotePort==0)
            {
                SocketResult result = SendTcpAsync(request.Tag, request.Buffer,
                        p => p.ProtocolId == request.ProtocolId,
                        p =>
                        {
                            Shoot_Response shoot = new Shoot_Response
                            {
                                Result = Convert.ToByte(SocketResult.Success),
                                Buffer = p.Buffer.GetRange(p.Offset, p.Size)
                            };
                            List<byte> responseBuffer = Protocol.Response(pack.TimeStamp, shoot);
                            pack.Handler.Send(pack.Socket, null, responseBuffer);
                        });

                if (result != SocketResult.Success)
                {
                    Shoot_Response response = new Shoot_Response
                    {
                        Result = Convert.ToByte(result),
                        Buffer = new List<byte>()
                    };
                    pack.Handler.Send(pack.Socket, null, Protocol.Response(pack.TimeStamp, response));
                }
            }
            else
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(request.RemoteIp, request.RemotePort);
                SocketResult result = SendUdpAsync(request.Tag,
                        remoteEndPoint,
                        request.Buffer,
                        p => p.ProtocolId == request.ProtocolId,
                        p =>
                        {
                            Shoot_Response shoot = new Shoot_Response
                            {
                                Result = Convert.ToByte(SocketResult.Success),
                                Buffer = p.Buffer.GetRange(p.Offset, p.Size)
                            };
                            List<byte> responseBuffer = Protocol.Response(pack.TimeStamp, shoot);
                            pack.Handler.Send(pack.Socket, pack.RemoteEndPoint, responseBuffer);
                        });

                if (result != SocketResult.Success)
                {
                    Shoot_Response response = new Shoot_Response
                    {
                        Result = Convert.ToByte(result),
                        Buffer = new List<byte>()
                    };
                    pack.Handler.Send(pack.Socket, remoteEndPoint, Protocol.Response(pack.TimeStamp, response));
                }
            }
        }
    }
}
