using System;
using System.Collections.Generic;
using System.Threading;
using Renci.SshNet;

namespace JabamiYumeko
{
    /// <summary>
    /// centos系统
    /// </summary>
    public abstract class CentOS: PerformanceChannel
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机信息</param>
        protected CentOS(Host host) : base(host)
        {
        }

        protected override void FillHostCore(SshClient client, Host host)
        {
            FillCpuCount(client, host);
            FillTop(client, host);
            FillDisk(client, host);
            FillDiskIO(client, host);
            FillNetIO(client, host);
        }

        protected override void FillServiceCore(SshClient client, Service service)
        {
            FillPid(client, service);
            FillStatus(client, service);
            FillDiskIO(client, service);
        }

        /// <summary>
        /// 供子类实现的top命令解析
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        protected abstract void FillTop(SshClient client, Host host);

        /// <summary>
        /// 获取主机cpu数
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        private void FillCpuCount(SshClient client,Host host)
        {
            string result=client.RunCommand("cat /proc/cpuinfo| grep \"physical id\"| wc -l").Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length > 0)
            {
                if (byte.TryParse(rows[0], out byte value))
                {
                    host.CPU_Count = value;
                }
            }
        }

        /// <summary>
        /// 获取硬盘信息
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        private void FillDisk(SshClient client, Host host)
        {
            string result = client.RunCommand("df").Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            List<HardDisk> disks=new List<HardDisk>();
            foreach (var row in rows)
            {
                string[] columns = row.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (columns[0].IndexOf("/dev", StringComparison.Ordinal) != -1)
                {
                    HardDisk disk = new HardDisk
                    {
                        Name = columns[0],
                        Mount = columns[5]
                    };
                    if(uint.TryParse(columns[1], out uint total))
                    {
                        disk.Total = total;
                    }
                    if (uint.TryParse(columns[2], out uint used))
                    {
                        disk.Used = used;
                    }
                    disks.Add(disk);
                }
            }

            host.Disks = disks;
        }

        /// <summary>
        /// 获取网络io
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        private void FillNetIO(SshClient client, Host host)
        {
            string result=client.RunCommand("cat /proc/net/dev").Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            ulong transmit1 = 0;
            ulong receive1 = 0;
            if (rows.Length > 3)
            {
                string[] columns = rows[3].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                ulong.TryParse(columns[1], out receive1);
                ulong.TryParse(columns[9], out transmit1);
            }
            Thread.Sleep(5000);

            result = client.RunCommand("cat /proc/net/dev").Execute();
            rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            ulong transmit2 = 0;
            ulong receive2 = 0;
            if (rows.Length > 3)
            {
                string[] columns = rows[3].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                ulong.TryParse(columns[1], out receive2);
                ulong.TryParse(columns[9], out transmit2);
            }

            host.Network_Transmit = Convert.ToUInt32(transmit2 - transmit1) / 5/1024;
            host.Network_Receive = Convert.ToUInt32(receive2 - receive1) / 5 / 1024;
        }

        /// <summary>
        /// 获取硬盘io
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        private void FillDiskIO(SshClient client, Host host)
        {
            string result=client.RunCommand("vmstat 1 10").Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            uint read = 0;
            uint write = 0;

            for (int i = 2; i < rows.Length; ++i)
            {
                string[] columns = rows[i].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (uint.TryParse(columns[8], out uint r))
                {
                    read += r;
                }
                if(uint.TryParse(columns[9], out uint w))
                {
                    write += w;
                }

            }
            read /= 10;
            write /= 10;

            host.Disk_Read = read;
            host.Disk_Write = write;
        }

        /// <summary>
        /// 供子类实现的获取服务进程号的函数
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="service">用于设置服务快照信息的实例</param>
        protected abstract void FillPid(SshClient client, Service service);

        /// <summary>
        /// 获取服务状态
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="service">用于设置服务快照信息的实例</param>
        private void FillStatus(SshClient client, Service service)
        {
            string result=client.RunCommand(string.Format("cat /proc/{0}/status",service.Pid)).Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                string[] columns = row.Split(new []{"\t"," "}, StringSplitOptions.RemoveEmptyEntries);
                if (columns[0].IndexOf("VmPeak", StringComparison.Ordinal) != -1)
                {
                    if (uint.TryParse(columns[1], out uint i))
                    {
                        service.Vm_Peak = i;
                    }
                }
                else if (columns[0].IndexOf("VmSize", StringComparison.Ordinal) != -1)
                {
                    if (uint.TryParse(columns[1], out uint i))
                    {
                        service.Vm_Used = i;
                    }
                }
                else if (columns[0].IndexOf("VmHWM", StringComparison.Ordinal) != -1)
                {
                    if (uint.TryParse(columns[1], out uint i))
                    {
                        service.Mem_Peak = i;
                    }
                }
                else if (columns[0].IndexOf("VmRSS", StringComparison.Ordinal) != -1)
                {
                    if (uint.TryParse(columns[1], out uint i))
                    {
                        service.Mem_Used = i;
                    }
                }
                else if (columns[0].IndexOf("Threads", StringComparison.Ordinal) != -1)
                {
                    if (short.TryParse(columns[1], out short i))
                    {
                        service.ThreadCount = i;
                    }
                }
            }
        }

        /// <summary>
        /// 获取服务硬盘io
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="service">用于设置服务快照信息的实例</param>
        private void FillDiskIO(SshClient client, Service service)
        {
            ulong write1 = 0;
            ulong read1 = 0;
            string result = client.RunCommand(string.Format("cat /proc/{0}/io", service.Pid)).Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                string[] columns=row.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length>= 2)
                {
                    if (columns[0].IndexOf("read_bytes", StringComparison.Ordinal) == 0)
                    {
                        ulong.TryParse(columns[1], out read1);
                    }
                    else if (columns[0].IndexOf("write_bytes", StringComparison.Ordinal) == 0)
                    {
                        ulong.TryParse(columns[1], out write1);
                    }
                }
            }
           
            Thread.Sleep(5000);

            ulong write2 = 0;
            ulong read2 = 0;
            result = client.RunCommand(string.Format("cat /proc/{0}/io", service.Pid)).Execute();
            rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                string[] columns = row.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length >= 2)
                {
                    if (columns[0].IndexOf("read_bytes", StringComparison.Ordinal) == 0)
                    {
                        ulong.TryParse(columns[1], out read2);
                    }
                    else if (columns[0].IndexOf("write_bytes", StringComparison.Ordinal) == 0)
                    {
                        ulong.TryParse(columns[1], out write2);
                    }
                }
            }

            service.Disk_Read = Convert.ToUInt32(read2 - read1) / 5/1024;
            service.Disk_Write = Convert.ToUInt32(write2 - write1) / 5/1024;
        }

    
    }
}
