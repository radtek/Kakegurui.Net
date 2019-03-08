using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

namespace SaotomeMeari
{
    /// <summary>
    /// 通知设备状态事件参数
    /// </summary>
    public class NoticingStatusEventArgs : EventArgs
    {
        public static byte Id => 0xA1;

        /// <summary>
        /// 设备地址
        /// </summary>
        [SerializeIndex(1)]
        public string Ip { get; set; }

        /// <summary>
        /// 设备状态
        /// </summary>
        [SerializeIndex(2)]
        public byte Status { get; set; }
    }

    /// <summary>
    /// 网络监测线程
    /// </summary>
    public class PingTask : TaskObject
    {

        /// <summary>
        /// 检测地址集合
        /// </summary>
        private readonly ConcurrentDictionary<uint, IPStatus> _ips=new ConcurrentDictionary<uint, IPStatus>();

        /// <summary>
        /// 通知设备状态事件
        /// </summary>
        public event EventHandler<NoticingStatusEventArgs> NoticingStatus;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="index">线程序号</param>
        public PingTask(int index) : 
            base("ping channel"+index)
        {
        }

        /// <summary>
        /// 添加ip
        /// </summary>
        /// <param name="pair">ip和设备状态</param>
        public void Add(KeyValuePair<uint,IPStatus> pair)
        {
            _ips[pair.Key]=pair.Value;
        }

        /// <summary>
        /// 清空线程内的所有ip
        /// </summary>
        public void Clear()
        {
            _ips.Clear();
        }

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                
                Ping ping = new Ping();
                foreach (var ip in _ips.ToArray())
                {
                    IPAddress address = new IPAddress(ip.Key);
                    PingReply reply=ping.Send(address);
                    IPStatus result = reply?.Status ?? IPStatus.Unknown;
                    
                    if (result == ip.Value)
                    {
                        LogPool.Logger.LogInformation("ping {0} {1}", address.ToString(), result);
                    }
                    else
                    {
                        LogPool.Logger.LogInformation("notice {0} {1}", address.ToString(), result);
                        NoticingStatus?.Invoke(this, new NoticingStatusEventArgs
                        {
                            Ip = address.ToString(),
                            Status = Convert.ToByte(result==IPStatus.Success?0x01:0x02)
                        });
                    }

                    _ips[ip.Key] = result;
                }
                Thread.Sleep(5000);
            }
        }
    }
}
