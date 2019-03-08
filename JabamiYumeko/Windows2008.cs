using System;
using System.IO;
using System.Text;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace JabamiYumeko
{
    public class Windows2008:PerformanceTask
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机信息</param>
        public Windows2008(Host host) : base(host)
        {
        }

        protected override void FillHostCore(SshClient client, Host host)
        {
            string result=client.RunCommand("wmic cpu get NumberOfLogicalProcessors").Execute();

            string[] rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 2)
            {
                string[] columns = rows[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length >= 1)
                {
                    if (int.TryParse(columns[0], out int i))
                    {
                        host.CPU_Count = Convert.ToByte(i * (rows.Length - 1));
                    }
                }
            }

            result = client.RunCommand("wmic memorychip get Capacity").Execute();
            rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 2)
            {
                if (long.TryParse(rows[1], out long l))
                {
                    host.Mem_Total = Convert.ToUInt32(l / 1024);
                }
            }

            result = client.RunCommand("wmic os get FreePhysicalMemory").Execute();
            rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 2)
            {
                if (long.TryParse(rows[1], out long l))
                {
                    host.Mem_Used = Convert.ToByte(host.Mem_Total - l);
                }
            }

            result = client.RunCommand("wmic LOGICALDISK get DriveType,FreeSpace,Name,Size,VolumeName").Execute();
            rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < rows.Length; ++i)
            {
                string[] columns = rows[i].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length >= 4)
                {
                    if (int.TryParse(columns[0], out int t))
                    {
                        if (t == 3)
                        {
                            HardDisk disk=new HardDisk();
                        
                            if (long.TryParse(columns[1], out long free)&&
                                long.TryParse(columns[3], out long to))
                            {
                                disk.Total = Convert.ToUInt32(to / 1024);
                                disk.Used = Convert.ToUInt32(to - free) / 1024;
                                
                            }
                            disk.Name = columns[2];
                            if (columns.Length >= 5)
                            {
                                Encoding gb2312 = Encoding.GetEncoding("gb2312");
                                byte[] temp = gb2312.GetBytes(columns[4]);
                                temp = Encoding.Convert(gb2312, Encoding.UTF8, temp);
                                disk.Mount=Encoding.UTF8.GetString(temp);
                            }
                            else
                            {
                                disk.Mount = "";
                            }
                            host.Disks.Add(disk);
                        }
                    }
                }
            }

            result = client.RunCommand("typeperf \"\\Processor(_Total)\\% Processor Time\" -sc 5").Execute();
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            float total = 0.0f;
            if (rows.Length >= 6)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"",StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 2)
                    {
                       
                        if (float.TryParse(columns[2], out float temp))
                        {
                            total += temp;
                        }
                  
                    }
                }
            }
            host.CPU_Used = total / 5.0f;

            result = client.RunCommand("typeperf \"\\PhysicalDisk(_Total)\\Disk Read Bytes/sec\" -sc 5").Execute();
            total = 0.0f;
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 6)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"", StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            total += temp;
                        }
                    }
                }
            }
            host.Disk_Read = Convert.ToUInt32(total / 1024.0f / 5.0f);

            result = client.RunCommand("typeperf \"\\PhysicalDisk(_Total)\\Disk Write Bytes/sec\" -sc 5").Execute();
            total = 0.0f;
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 6)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"",StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            total += temp;
                        }                        
                    }
                }
            }
            host.Disk_Write =Convert.ToUInt32(total / 1024.0f / 5.0f);

            result = client.RunCommand("typeperf \"\\Network Interface(*)\\Bytes Received/sec\" -sc 5").Execute();
            total = 0.0f;
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 6)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"",StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            total += temp;
                        }
                    }
                }
            }
            host.Network_Receive = Convert.ToUInt32(total / 1024.0f / 5.0f);

            result = client.RunCommand("typeperf \"\\Network Interface(*)\\Bytes Sent/sec\" -sc 5").Execute();
            total = 0.0f;
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 6)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"", StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            total += temp;
                        }
                    }
                }
            }
            host.Network_Transmit = Convert.ToUInt32(total / 1024.0f / 5.0f);
        }

        protected override void FillServiceCore(SshClient client, Service service)
        {
            string result=client.RunCommand("wmic cpu get NumberOfLogicalProcessors").Execute();
            
            int cpuCount = 0;
            string[] rows = result.Split("\r\r\n",StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 2)
            {
                string[] columns = rows[1].Split(" ",StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length!=0)
                {
                    if(int.TryParse(columns[0], out int numberOfLogicalProcessors))
                    {
                        cpuCount = numberOfLogicalProcessors * (rows.Length - 1);
                    }
                }
            }
            if (cpuCount == 0)
            {
                LogPool.Logger.LogWarning("{0} cpu count is 0", service.Ip);
                return;
            }

            result = client.RunCommand(
                "wmic SERVICE where name=\"{0}\" get ProcessId,StartMode,State").Execute();
            rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 2)
            {
                string[] columns = rows[1].Split("  ",StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length>= 3)
                {
                    if (int.TryParse(columns[0], out int p))
                    {
                        service.Pid = p;
                    }
                    service.Config = columns[1]=="Auto" ? (byte)ServiceConfig.Auto : (byte)ServiceConfig.Demand;
                    service.Status = columns[2]=="Running" ? (byte)ServiceStatus.Running : (byte)ServiceStatus.Stopped;
                }
            }
            else
            {
                return;
            }

            result = client.RunCommand(
                string.Format("wmic process where processId={0} get Name,PeakVirtualSize,PeakWorkingSetSize,ThreadCount,VirtualSize,WorkingSetSize", service.Pid)).Execute();
            rows = result.Split("\r\r\n", StringSplitOptions.RemoveEmptyEntries);
            string processName=null;
            if (rows.Length >= 2)
            {
                string[] columns = rows[1].Split("  ",StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length >= 5)
                {
                    processName = Path.GetFileName(columns[0]);
                    if (long.TryParse(columns[1], out long vp))
                    {
                        service.Vm_Peak = Convert.ToUInt32(vp / 1024);
                    }
                    if (uint.TryParse(columns[2], out uint mp))
                    {
                        service.Mem_Peak = mp;
                    }
                    if (short.TryParse(columns[3], out short tc))
                    {
                        service.ThreadCount = tc;
                    }
                    if (long.TryParse(columns[4], out long vu))
                    {
                        service.Vm_Used = Convert.ToUInt32(vu / 1024);
                    }
                    if (long.TryParse(columns[5], out long mu))
                    {
                        service.Mem_Used = Convert.ToUInt32(mu / 1024);
                    }
                }
            }

            result = client.RunCommand(
                string.Format("typeperf \"\\Process({0})\\% Processor Time\" -sc 5", processName)).Execute();
            float total = 0.0f;
            rows = result.Split("\r\n",StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length >= 9)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"",StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            if (temp < 0.0f)
                            {
                                return;
                            }
                            total += temp;
                        }
                    }
                }
                service.CPU_Used = total / Convert.ToSingle(cpuCount);
            }

            result = client.RunCommand(
                string.Format("typeperf \"\\Process({0})\\IO Write Bytes/sec\" -sc 5", processName)).Execute();
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            total = 0.0f;
            if (rows.Length >= 9)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"",StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            if (temp < 0.0f)
                            {
                                return;
                            }
                            total += temp;
                        }
                    }
                }
                service.Disk_Write = Convert.ToUInt32(total / 5);
            }

            result = client.RunCommand(
                string.Format("typeperf \"\\Process({0})\\IO Read Bytes/sec\" -sc 5", processName)).Execute();
            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            total = 0.0f;
            if (rows.Length >= 9)
            {
                for (int i = 1; i < 6; ++i)
                {
                    string[] columns = rows[i].Split("\"", StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3)
                    {
                        if (float.TryParse(columns[2], out float temp))
                        {
                            if (temp < 0.0f)
                            {
                                return;
                            }
                            total += temp;
                        }
                    }
                }
                service.Disk_Read = Convert.ToUInt32(total / 5);
            }
        }

        protected override string ControlServiceCore(SshClient client, ControlService_Request request)
        {
            if (request.Op == (byte)ServiceConfig.Restart)
            {
                string stopCmd = string.Format("sc stop {0}", request.Name);
                client.RunCommand(stopCmd).Execute();
                string startCmd = string.Format("sc start {0}", request.Name);
                client.RunCommand(startCmd).Execute();
                return Success;
            }

            string cmd;
            if (request.Op == (byte)ServiceConfig.Start)
            {
                cmd = string.Format("sc start {0}", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Stop)
            {
                cmd = string.Format("sc stop {0}", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Auto)
            {
                cmd = string.Format("sc config {0} start=auto", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Demand)
            {
                cmd = string.Format("sc config {0} start=demand", request.Name);
            }
            else
            {
                return UnknownOP;
            }
            SshCommand command = client.RunCommand(cmd);
            command.Execute();
            return command.ExitStatus == 0 ? Success : command.Error;
        }
    }
}
