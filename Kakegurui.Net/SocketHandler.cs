using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

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
        /// 请求协议
        /// </summary>
        Request,
        /// <summary>
        /// 响应协议
        /// </summary>
        Response
    };

    /// <summary>
    /// 接收到协议事件参数
    /// </summary>
    public class GotProtocolEventArgs : EventArgs
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// 发送字节流
        /// </summary>
        public List<byte> Buffer { get; set; }

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
        public int ProtocolId { get; set; }

        /// <summary>
        /// 发送时间戳
        /// </summary>
        public long TimeStamp { get; set; }

        /// <summary>
        /// 响应字节流
        /// </summary>
        public byte[] ResponseBuffer { get; set; }
    }

    /// <summary>
    /// 套接字处理类
    /// </summary>
    public abstract class SocketHandler
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        protected ILogger _logger;

        /// <summary>
        /// 异步处理列表
        /// </summary>
        private readonly LinkedList<ReceiveAsyncHandler> _handlers=new LinkedList<ReceiveAsyncHandler>();

        /// <summary>
        /// 残包
        /// </summary>
        private readonly List<byte> _residueBuffer=new List<byte>();

        /// <summary>
        /// 收到协议事件
        /// </summary>
        public event EventHandler<GotProtocolEventArgs> GotProtocol;

        /// <summary>
        /// 构造函数
        /// </summary>
        protected SocketHandler()
        {
            TransmitSize = 0;
            ReceiveSize = 0;
            _logger = LogPool.Logger;
        }

        /// <summary>
        /// 发送总字节数
        /// </summary>
        public ulong TransmitSize { get; private set; }

        /// <summary>
        /// 接收总字节数
        /// </summary>
        public ulong ReceiveSize { get; private set; }

        /// <summary>
        /// tcp发送
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例</param>
        /// <returns>发送结果</returns>
        public SocketResult SendTcp(Socket socket, byte[] buffer, ReceiveAsyncHandler handler=null)
        {
            TransmitSize += Convert.ToUInt32(buffer.Length);
            if (handler != null)
            {
                AutoLock.Lock(this, () =>
                {
                    _handlers.AddLast(handler);
                });
            }
            _logger?.LogInformation("{0} {1} {2} {3}",socket.Handle, "-", buffer.Length, ByteConvert.ToHex(buffer));
     
            if (buffer.Length == 0)
            {
                return SocketResult.SendFailed;
            }

            try
            {
                int written = 0;
                while (written != buffer.Length)
                {
                    int n;
                    if ((n = socket.Send(buffer, written, buffer.Length - written, SocketFlags.None)) <= 0)
                    {
                        return SocketResult.SendFailed;
                    }
                    written += n;
                }
                return SocketResult.Success;
            }
            catch (Exception)
            {
                return SocketResult.SendFailed;
            }
        }

        /// <summary>
        /// udp发送
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">远程地址</param>
        /// <param name="buffer">字节流</param>
        /// <param name="handler">异步接收处理实例</param>
        /// <returns></returns>
        public SocketResult SendUdp(Socket socket, IPEndPoint remoteEndPoint, byte[] buffer, ReceiveAsyncHandler handler = null)
        {
            TransmitSize += Convert.ToUInt32(buffer.Length);
            if (handler != null)
            {
                AutoLock.Lock(this, ()=>
                {
                    _handlers.AddLast(handler);
                });
            }
            _logger?.LogInformation("{0} {1} {2} {3} {4}", socket.Handle,remoteEndPoint.ToString(), "-", buffer.Length, ByteConvert.ToHex(buffer));

            if (buffer.Length == 0)
            {
                return SocketResult.SendFailed;
            }

            try
            {
                int written = 0;
                while (written != buffer.Length)
                {
                    int n;
                    if ((n = socket.SendTo(buffer, written, buffer.Length - written, SocketFlags.None, remoteEndPoint)) <= 0)
                    {
                        return SocketResult.SendFailed;
                    }
                    written += n;
                }
                return SocketResult.Success;
            }
            catch (Exception)
            {
                return SocketResult.SendFailed;
            } 
        }

        /// <summary>
        /// 设置日志
        /// </summary>
        /// <param name="logName">日志名</param>
        public void SetLogger(string logName)
        {
            _logger = new FileLogger(new AllFilter(), logName);
        }

        /// <summary>
        /// 复制处理实例
        /// </summary>
        /// <returns>处理实例</returns>
        public SocketHandler Clone()
        {
            SocketHandler handler = (SocketHandler)(GetType().GetConstructors()[0].Invoke(new object[] { }));
            handler.GotProtocol = GotProtocol;
            return handler;
        }

        /// <summary>
        /// 处理接收字节流
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="buffer">字节流</param>
        /// <param name="size">字节流长度</param>
        /// <param name="remoteEndPoint">远程地址</param>
        public void Handle(Socket socket, byte[] buffer,int size, IPEndPoint remoteEndPoint=null)
        {
            ReceiveSize += Convert.ToUInt32(size);

            if (remoteEndPoint==null)
            {
                _logger?.LogInformation("{0} {1} {2} {3}",socket.Handle, "+", size, ByteConvert.ToHex(buffer,size));
            }
            else
            {
                _logger?.LogInformation("{0} {1} {2} {3} {4}", socket.Handle, remoteEndPoint.ToString(), "+", size, ByteConvert.ToHex(buffer,size));
            }

            _residueBuffer.AddRange(buffer.Take(size));


            int offset = 0;
            do
            {
                GotProtocolEventArgs packet = Unpack(socket,remoteEndPoint, _residueBuffer,offset);
                if (packet.Result == AnalysisResult.Request)
                {
                    packet.Socket = socket;
                    packet.Buffer = _residueBuffer;
                    GotProtocol?.Invoke(this, packet);
                    if (packet.ResponseBuffer != null)
                    {
                        if (socket.ProtocolType == ProtocolType.Tcp)
                        {
                            SendTcp(socket, packet.ResponseBuffer);
                        }
                        else
                        {
                            SendUdp(socket, remoteEndPoint, packet.ResponseBuffer);
                        }
                    }
                }
                else if (packet.Result == AnalysisResult.Response)
                {
                    if (_handlers.Count != 0)
                    {
                        AutoLock.Lock(this, () =>
                        {
                            LinkedListNode<ReceiveAsyncHandler> node = _handlers.First;
                            while (node != null)
                            {
                                if (node.Value.IsCompleted())
                                {
                                    LinkedListNode<ReceiveAsyncHandler> temp = node;
                                    node = node.Next;
                                    _handlers.Remove(temp);
                                }
                                else
                                {
                                    if (node.Value.ProtocolId == packet.ProtocolId && node.Value.TimeStamp == packet.TimeStamp)
                                    {
                                        node.Value.Handle(_residueBuffer, packet.Offset, packet.Size);
                                    }
                                    node = node.Next;
                                }
                            }
                        });
                    }
                }
                else if (packet.Result == AnalysisResult.Half)
                {
                    _residueBuffer.RemoveRange(0,offset);
                    return;
                }
                offset += packet.Offset + packet.Size;

            } while (offset < _residueBuffer.Count);
            _residueBuffer.Clear();
        }

        /// <summary>
        /// 拆包
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">远程地址，udp有效</param>
        /// <param name="buffer">字节流</param>
        /// <param name="offset">偏移量</param>
        /// <returns>处理结果</returns>
        protected abstract GotProtocolEventArgs Unpack(Socket socket,IPEndPoint remoteEndPoint,List<byte> buffer,int offset);

    };
}
