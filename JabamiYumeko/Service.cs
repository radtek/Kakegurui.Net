using System;
using Kakegurui.Core;

namespace JabamiYumeko
{
    public enum ServiceConfig : byte
    {
        Start = 0,
        Stop = 1,
        Restart = 2,
        Auto = 3,
        Demand = 4
    };

    //服务操作
    public enum ServiceStatus : byte
    {
        Unknown = 0,
        Running = 1,
        Stopped = 2
    };

    public class Service : EventArgs
    {
        public static byte Id => 0xB3;

        //服务主机地址
        [SerializeIndex(1)]
        public string Ip{get;set;}
        //服务名称
        [SerializeIndex(2)]
        public string Name {get;set;}

        //当前查询的时间戳
        [SerializeIndex(3)]
        public long TimeStamp {get;set;}
        //服务状态
        [SerializeIndex(4)]
        public byte Status {get;set;}
        //服务参数
        [SerializeIndex(5)]
        public byte Config {get;set;}
        //服务进程号
        [SerializeIndex(6)]
        public int Pid {get;set;}
        //线程数
        [SerializeIndex(7)]
        public short ThreadCount { get; set; }
        //cpu使用率
        [SerializeIndex(8)]
        public float CPU_Used {get;set;}
        //虚拟内存峰值(KB)
        [SerializeIndex(9)]
        public uint Vm_Peak {get;set;}
        //虚拟内存(KB)
        [SerializeIndex(10)]
        public uint Vm_Used { get; set; }
        //物理内存峰值(KB)
        [SerializeIndex(11)]
        public uint Mem_Peak {get;set;}
        //物理内存(KB)
        [SerializeIndex(12)]
        public uint Mem_Used {get;set;}
        //磁盘写入kb/s
        [SerializeIndex(13)]
        public uint Disk_Write {get;set;}
        //磁盘读取kb/s
        [SerializeIndex(14)]
        public uint Disk_Read {get;set;}
        //网络发送(kb/s)
        [SerializeIndex(15)]
        public uint Network_Transmit {get;set;}
        //网络接收(kb/s)
        [SerializeIndex(16)]
        public uint Network_Receive {get;set;}
    }

    public class ControlService_Request
    {
        public static byte Id => 0xB5;
        [SerializeIndex(1)]
        public string Ip { get; set; }
        [SerializeIndex(2)]
        public string Name { get; set; }
        [SerializeIndex(3)]
        public byte Op { get; set; }
    }

    public class ControlService_Response
    {
        public static byte Id => 0xB6;
        [SerializeIndex(1)]
        public string Result { get; set; }
    }
}
