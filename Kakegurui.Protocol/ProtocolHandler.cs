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
    public class ProtocolHandler:SocketHandler
    {
        protected override GotProtocolEventArgs Unpack(Socket socket,IPEndPoint remoteEndPoint, List<byte> buffer, int start)
        {
            int head=buffer.FindIndex(b => b == ProtocolHead.Tag);
     
            //未找到协议
            if (head == -1)
            {
                return new GotProtocolEventArgs
                {
                    Result = AnalysisResult.Empty,
                    Offset = 0,
                    Size = buffer.Count- start
                };
            }
            else
            {
                int offset = start - head;
                int lessSize = buffer.Count - offset - head;
                //长度小于协议头
                if (lessSize < ProtocolHead.HeadSize)
                {
                    return new GotProtocolEventArgs
                    {
                        Result = AnalysisResult.Half,
                        Offset = offset,
                        Size = lessSize
                    };
                }
                else
                {
      
                    ProtocolHead protocolHead=new ProtocolHead();
                    ByteFormatter.Deserialize(protocolHead,buffer,1);

                    if (lessSize < protocolHead.ContentSize + ProtocolHead.HeadSize)
                    {
                        return new GotProtocolEventArgs
                        {
                            Result = AnalysisResult.Half,
                            Offset = offset,
                            Size = lessSize
                        };
                    }
                    else
                    {
                        return new GotProtocolEventArgs
                        {
                            Result = protocolHead.Id % 2 != 0 ? AnalysisResult.Request : AnalysisResult.Response,
                            Offset = offset,
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
