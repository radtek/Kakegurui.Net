using System;
using Renci.SshNet;

namespace JabamiYumeko
{
    /// <summary>
    /// CentOS7
    /// </summary>
    public class CentOS7:CentOS
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机信息</param>
        public CentOS7(Host host) : base(host)
        {
        }

        protected override void FillTop(SshClient client, Host host)
        {
            string result = client.RunCommand("top -bn 1").Execute();
            int index = result.IndexOf("load average", StringComparison.Ordinal);
            if (index != -1)
            {
                int end1 = result.IndexOf(',', index);
                if (float.TryParse(result.Substring(index + 13, end1 - index - 13), out float f5))
                {
                    host.Load_5 = f5;
                }

                int end2 = result.IndexOf(',', end1 + 1);
                if (float.TryParse(result.Substring(end1 + 1, end2 - end1 - 1), out float f10))
                {
                    host.Load_10 = f10;
                }

                int end3 = result.IndexOf('\n', end2 + 2);
                if (float.TryParse(result.Substring(end2 + 2, end3 - end2 - 2), out float f15))
                {
                    host.Load_15 = f15;
                }
            }

            index = result.IndexOf("ni,", StringComparison.Ordinal);
            if (index != -1)
            {
                int end = result.IndexOf("id", index, StringComparison.Ordinal);
                if (float.TryParse(result.Substring(index + 4, end - index - 4), out float f))
                {
                    host.CPU_Used = 100.0f - f;
                }
            }

            index = result.IndexOf("KiB Mem", StringComparison.Ordinal);
            if (index != -1)
            {
                int end = result.IndexOf("total,", index, StringComparison.Ordinal);
                if (uint.TryParse(result.Substring(index + 9, end - index - 9), out uint i1))
                {
                    host.Mem_Total = i1;
                }
                index = result.IndexOf("free", end, StringComparison.Ordinal);
                end = result.IndexOf("used", index, StringComparison.Ordinal);
                if(uint.TryParse(result.Substring(index + 7, end - index - 7),out uint i2))
                {
                    host.Mem_Used = i2;
                }
            }
        }

        protected override void FillPid(SshClient client, Service service)
        {
            string result=client.RunCommand(string.Format("systemctl status {0}", service.Name)).Execute();
            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (string row in rows)
            {
                if (row.IndexOf("Main PID:", StringComparison.Ordinal) != -1)
                {
                    string[] columns = row.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length >= 3&&int.TryParse(columns[2],out int p))
                    {
                        service.Pid = p;
                    }
                }
                else if (row.IndexOf("Active:", StringComparison.Ordinal) != -1)
                {
                    service.Status = row.IndexOf("running", StringComparison.Ordinal) ==-1 ? (byte)ServiceStatus.Stopped : (byte)ServiceStatus.Running;
                }
                else if (row.IndexOf("Loaded:", StringComparison.Ordinal) != -1)
                {
                    string[] columns = row.Split(";", StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length > 1)
                    {
                        service.Config = columns[1].IndexOf("enabled", StringComparison.Ordinal) != -1 ? (byte)ServiceConfig.Auto : (byte)ServiceConfig.Demand;
                    }
                }
            }
            
            //即使服务停止也可以读取出pid但是无效
            if (service.Status == (byte)ServiceStatus.Running)
            {
                result=client.RunCommand(string.Format("ps -aux|grep {0} |awk -- '{{print $3}}'", service.Pid)).Execute();

                rows =result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length>0)
                {
                    if (float.TryParse(rows[0], out float f))
                    {
                        service.CPU_Used = f;
                    }
                }
            }
            else
            {
                service.Pid = 0;
            }
        }

        protected override string ControlServiceCore(SshClient client, ControlService_Request request)
        {
            string cmd;
            if (request.Op == (byte)ServiceConfig.Start)
            {
                cmd = string.Format("systemctl start {0}", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Stop)
            {
                cmd = string.Format("systemctl stop {0}", request.Name);
            }
            else if(request.Op==(byte)ServiceConfig.Restart)
            {
                cmd = string.Format("systemctl restart {0}", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Auto)
            {
                cmd = string.Format("systemctl enable {0}", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Demand)
            {
                cmd = string.Format("systemctl disable {0}", request.Name);
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
