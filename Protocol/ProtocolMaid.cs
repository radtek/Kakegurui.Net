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

        }


        /// <summary>
        /// 添加tcp监听端口
        /// </summary>
        /// <param name="port">监听端口</param>
        public SocketChannel AddListenEndPoint(int port)
        {
            SocketChannel channel = AddListenEndPoint(new ProtocolHandler(),port);
            channel.Where(p => p.ProtocolId == Convert.ToUInt16(ProtocolId.CollectStatus))
                .Subscribe(CollectStatus);
            channel.Where(p => p.ProtocolId == Convert.ToUInt16(ProtocolId.Shoot))
                .Subscribe(Shoot);
            return channel;
        }

        /// <summary>
        /// 添加udp绑定端口
        /// </summary>
        /// <param name="port">监听端口</param>
        public SocketChannel AddBindEndPoint(int port)
        {
            SocketChannel channel = AddUdpServer(new ProtocolHandler(), port);
            channel.Where(p => p.ProtocolId == Convert.ToUInt16(ProtocolId.CollectStatus))
                .Subscribe(CollectStatus);
            channel.Where(p => p.ProtocolId == Convert.ToUInt16(ProtocolId.Shoot))
                .Subscribe(Shoot);
            return channel;
        }

        /// <summary>
        /// 收集状态
        /// </summary>
        /// <param name="args">协议包</param>
        protected void CollectStatus(ReceivedEventArgs args)
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
                    Transmit = socket.Value.TransmitSize,
                    Receive = socket.Value.ReceiveSize,
                    LocalIp = socket.Value.LocalEndPoint==null?0:BitConverter.ToUInt32(socket.Value.LocalEndPoint.Address.GetAddressBytes(), 0),
                    LocalPort = Convert.ToUInt16(socket.Value.LocalEndPoint?.Port ?? 0),
                    RemoteIp = socket.Value.RemoteEndPoint==null?0: BitConverter.ToUInt32(socket.Value.RemoteEndPoint.Address.GetAddressBytes(), 0),
                    RemotePort = Convert.ToUInt16(socket.Value.RemoteEndPoint?.Port ?? 0)
                };
                cs.SocketInfo.Add(status);
            }
            args.Channel.Send(args.RemoteEndPoint, Protocol.Response(args.TimeStamp, cs));
        }

        /// <summary>
        /// 转发协议
        /// </summary>
        /// <param name="args">协议包</param>
        protected void Shoot(ReceivedEventArgs args)
        {
            Shoot_Request request = new Shoot_Request();
            ByteFormatter.Deserialize(request, args.Buffer, ProtocolHead.HeadSize);
            IPEndPoint remoteEndPoint = new IPEndPoint(request.RemoteIp, request.RemotePort);
            SocketResult result = SendAsync(request.Tag,
                remoteEndPoint,
                request.Buffer,
                p => p.ProtocolId == request.ProtocolId,
                p =>
                {
                    Shoot_Response shoot = new Shoot_Response
                    {
                        Result = Convert.ToByte(SocketResult.Success),
                        Buffer = p.Buffer
                    };
                    List<byte> responseBuffer = Protocol.Response(args.TimeStamp, shoot);
                    args.Channel.Send(args.RemoteEndPoint, responseBuffer);
                });

            if (result != SocketResult.Success)
            {
                Shoot_Response response = new Shoot_Response
                {
                    Result = Convert.ToByte(result),
                    Buffer = new List<byte>()
                };
                args.Channel.Send(remoteEndPoint, Protocol.Response(args.TimeStamp, response));
            }
        }
    }
}
