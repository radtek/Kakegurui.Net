using System;
using System.Collections.Generic;
using Kakegurui.Core;

namespace JabamiYumeko
{
    /// <summary>
    /// 操作系统
    /// </summary>
    public enum OperationSystem 
    {
        Unknown = -1,
        Windows2008 = 0,
        CentOS7 = 1,
        CentOS6 = 2
    };

    /// <summary>
    /// 服务状态
    /// </summary>
    public enum HostStatus : byte
    {
        Connection = 0,
        Disconnection = 1
    };

    /// <summary>
    /// 硬盘
    /// </summary>
    public class HardDisk
    {
        /// <summary>
        /// 硬盘名
        /// </summary>
        [SerializeIndex(1)]
        public string Name { get; set; }

        /// <summary>
        /// 硬盘挂载或描述
        /// </summary>
        [SerializeIndex(2)]
        public string Mount { get; set; }

        /// <summary>
        /// 硬盘总量(KB)
        /// </summary>
        [SerializeIndex(3)]
        public uint Total { get; set; }

        /// <summary>
        /// 硬盘使用(KB)
        /// </summary>
        [SerializeIndex(4)]
        public uint Used { get; set; }
    }
    /// <summary>
    /// 主机
    /// </summary>
    public class Host : EventArgs
    {
        public static byte Id => 0xB1;

        /// <summary>
        /// 主机地址
        /// </summary>
        [SerializeIndex(1)]
        public string Ip { get; set; }

        /// <summary>
        /// 主机端口
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 登陆密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 操作系统
        /// </summary>
        public OperationSystem OS { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [SerializeIndex(2)]
        public long TimeStamp { get; set; }

        /// <summary>
        /// 主机状态
        /// </summary>
        [SerializeIndex(3)]
        public byte Status { get; set; }

        /// <summary>
        /// 5分钟负载
        /// </summary>
        [SerializeIndex(4)]
        public float Load_5 { get; set; }

        /// <summary>
        /// 10分钟负载
        /// </summary>
        [SerializeIndex(5)]
        public float Load_10 { get; set; }

        /// <summary>
        /// 15分钟负载
        /// </summary>
        [SerializeIndex(6)]
        public float Load_15 { get; set; }

        /// <summary>
        /// cpu内核数
        /// </summary>
        [SerializeIndex(7)]
        public byte CPU_Count { get; set; }

        /// <summary>
        /// cpu使用百分比
        /// </summary>
        [SerializeIndex(8)]
        public float CPU_Used { get; set; }

        /// <summary>
        /// 内存总量(kb)
        /// </summary>
        [SerializeIndex(9)]
        public uint Mem_Total { get; set; }

        /// <summary>
        /// 内存使用量(kb)
        /// </summary>
        [SerializeIndex(10)]
        public uint Mem_Used { get; set; }

        /// <summary>
        /// 磁盘读取(kb/s)
        /// </summary>
        [SerializeIndex(11)]
        public uint Disk_Read { get; set; }

        /// <summary>
        /// 磁盘写入(kb/s)
        /// </summary>
        [SerializeIndex(12)]
        public uint Disk_Write { get; set; }

        /// <summary>
        /// 网络发送(kb/s)
        /// </summary>
        [SerializeIndex(13)]
        public uint Network_Transmit { get; set; }

        /// <summary>
        /// 网络接收(kb/s)
        /// </summary>
        [SerializeIndex(14)]
        public uint Network_Receive { get; set; }

        /// <summary>
        /// 硬盘集合
        /// </summary>
        [SerializeIndex(15)]
        public List<HardDisk> Disks { get; set; }
    }
};

