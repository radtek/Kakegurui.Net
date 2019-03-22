using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
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
        /// 全包
        /// </summary>
        Full
    };

    /// <summary>
    /// 接收到协议事件参数
    /// </summary>
    public class SocketPack
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// udp远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 套接字操作实例
        /// </summary>
        public SocketHandler Handler { get; set; }

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
    }

    /// <summary>
    /// 套接字处理类
    /// </summary>
    public abstract class SocketHandler:IObservable<SocketPack>
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        protected ILogger _logger;

        /// <summary>
        /// 残包
        /// </summary>
        private readonly List<byte> _residueBuffer=new List<byte>();

        /// <summary>
        /// 订阅接收字节流
        /// </summary>
        private ConcurrentDictionary<IObserver<SocketPack>, object> _observers= new ConcurrentDictionary<IObserver<SocketPack>, object>();

        /// <summary>
        /// 构造函数
        /// </summary>
        protected SocketHandler()
        {
            TransmitSize = 0;
            ReceiveSize = 0;
        }
        #region 实现IObservable
        public IDisposable Subscribe(IObserver<SocketPack> observer)
        {
            _observers[observer]=null;
            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private readonly ConcurrentDictionary<IObserver<SocketPack>, object> _observers;
            private readonly IObserver<SocketPack> _observer;

            public Unsubscriber(ConcurrentDictionary<IObserver<SocketPack>, object> observers, IObserver<SocketPack> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null)
                    _observers.TryRemove(_observer,out object obj);
            }
        }
        #endregion

        /// <summary>
        /// 发送总字节数
        /// </summary>
        public ulong TransmitSize { get; private set; }

        /// <summary>
        /// 接收总字节数
        /// </summary>
        public ulong ReceiveSize { get; private set; }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志格式</param>
        /// <param name="args">日志内容</param>
        private void Log(string message, params object[] args)
        {
            LogPool.Logger.LogDebug(message, args);
            _logger?.LogInformation(message, args);
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="action">成功响应后的异步回调，默认为null，表示同步等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult Send(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer, Func<SocketPack, bool> match = null, Action<SocketPack> action = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            if (match == null)
            {
                return SendCore(socket, remoteEndPoint, buffer);
            }
            else
            {
                if (action == null)
                {
                    AutoResetEvent _event = new AutoResetEvent(false);
                    IDisposable disposable = this.Where(match)
                        .Subscribe(pack =>
                        {
                            _event.Set();
                            receiveBuffer?.AddRange(pack.Buffer.GetRange(pack.Offset, pack.Size));
                        });
                    SocketResult result = SendCore(socket, remoteEndPoint, buffer);
                    if (result == SocketResult.Success)
                    {
                        result = _event.WaitOne(timeout) ? SocketResult.Success : SocketResult.Timeout;
                    }
                    disposable.Dispose();
                    return result;
                }
                else
                {
                    IDisposable disposable = null;
                    disposable = this.Where(match)
                        .Timeout(TimeSpan.FromMilliseconds(timeout))
                        .Subscribe(
                            pack =>
                            {
                                action.Invoke(pack);
                                disposable.Dispose();
                            },
                            ex =>
                            {
                                disposable.Dispose();
                            });
                    return SendCore(socket, remoteEndPoint, buffer);
                }
            }
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="socket">套接字</param>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        private SocketResult SendCore(Socket socket, IPEndPoint remoteEndPoint, List<byte> buffer)
        {
            byte[] temp = buffer.ToArray();
            if (temp.Length == 0)
            {
                return SocketResult.SendFailed;
            }
            TransmitSize += Convert.ToUInt32(temp.Length);
            try
            {
                if (remoteEndPoint == null)
                {
                    Log("{0} {1} {2} {3}", socket.Handle, "-", temp.Length, ByteConvert.ToHex(temp));
                    int written = 0;
                    while (written != temp.Length)
                    {
                        int n;
                        if ((n = socket.Send(temp, written, temp.Length - written, SocketFlags.None)) <= 0)
                        {
                            return SocketResult.SendFailed;
                        }
                        written += n;
                    }
                }
                else
                {
                    Log("{0} {1} {2} {3} {4}", socket.Handle, remoteEndPoint.ToString(), "-", temp.Length, ByteConvert.ToHex(temp));
                    int written = 0;
                    while (written != temp.Length)
                    {
                        int n;
                        if ((n = socket.SendTo(temp, written, temp.Length - written, SocketFlags.None, remoteEndPoint)) <= 0)
                        {
                            return SocketResult.SendFailed;
                        }
                        written += n;
                    }
                }
                return SocketResult.Success;
            }
            catch (SocketException)
            {
                return SocketResult.SendFailed;
            }
            catch (ObjectDisposedException)
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
        public virtual SocketHandler Clone()
        {
            SocketHandler handler = (SocketHandler)(GetType().GetConstructors()[0].Invoke(new object[] { }));
            handler._observers = _observers;
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
                Log("{0} {1} {2} {3}",socket.Handle, "+", size, ByteConvert.ToHex(buffer,size));
            }
            else
            {
                Log("{0} {1} {2} {3} {4}", socket.Handle, remoteEndPoint.ToString(), "+", size, ByteConvert.ToHex(buffer,size));
            }

            _residueBuffer.AddRange(buffer.Take(size));


            int offset = 0;
            do
            {
                SocketPack packet = Unpack(socket,remoteEndPoint, _residueBuffer,offset);
                if (packet.Result == AnalysisResult.Full)
                {
                    packet.Socket = socket;
                    packet.RemoteEndPoint = remoteEndPoint;
                    packet.Handler = this;
                    packet.Buffer = _residueBuffer;
                    foreach (var pair in _observers)
                    {
                        pair.Key.OnNext(packet);
                    }
                }
                else if (packet.Result == AnalysisResult.Half)
                {
                    _residueBuffer.RemoveRange(0, offset);
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
        protected abstract SocketPack Unpack(Socket socket,IPEndPoint remoteEndPoint,List<byte> buffer,int offset);

    };
}
