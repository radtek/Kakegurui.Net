using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Kakegurui.Core;
using Kakegurui.Net;

namespace Kakegurui.Protocol
{
    /// <summary>
    /// 协议处理
    /// </summary>
    public class ProtocolHandler:ISocketHandler
    {
        public SocketPack Unpack(Socket socket,IPEndPoint remoteEndPoint, List<byte> buffer, int offset)
        {
            int head=buffer.FindIndex(offset,b => b == ProtocolHead.Tag);
     
            //未找到协议
            if (head == -1)
            {
                return new SocketPack
                {
                    Result = AnalysisResult.Empty,
                    Offset = offset,
                    Size = buffer.Count- offset
                };
            }
            else
            {
                int lessSize = buffer.Count - head;
                //长度小于协议头
                if (lessSize < ProtocolHead.HeadSize)
                {
                    return new SocketPack
                    {
                        Result = AnalysisResult.Half,
                        Offset = head,
                        Size = lessSize
                    };
                }
                else
                {
      
                    ProtocolHead protocolHead=new ProtocolHead();
                    ByteFormatter.Deserialize(protocolHead, buffer, head + 1);
                    if (lessSize < protocolHead.ContentSize + ProtocolHead.HeadSize)
                    {
                        return new SocketPack
                        {
                            Result = AnalysisResult.Half,
                            Offset = head,
                            Size = lessSize
                        };
                    }
                    else
                    {
                        return new SocketPack
                        {
                            Result = AnalysisResult.Full,
                            Offset = head,
                            Size = protocolHead.ContentSize + ProtocolHead.HeadSize,
                            ProtocolId = protocolHead.Id,
                            TimeStamp = protocolHead.TimeStamp
                        };
                    }
                }
            }
        }
    }
}
