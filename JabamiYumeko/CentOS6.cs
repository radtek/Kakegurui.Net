using System;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet;

namespace JabamiYumeko
{
    /// <summary>
    /// CentOS6
    /// </summary>
    public class CentOS6:CentOS
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机信息</param>
        public CentOS6(Host host) : base(host)
        {
        }

        protected override void FillTop(SshClient client, Host host)
        {
            string result = client.RunCommand("top -bn 1").Execute();

 
            int index = result.IndexOf("load average", StringComparison.Ordinal);
            if (index != -1)
            {
                int end1 = result.IndexOf(',', index);
                if(float.TryParse(result.Substring(index + 13, end1 - index - 13),out float f5))
                {
                    host.Load_5 = f5;
                }
                
                int end2 = result.IndexOf(',', end1 + 1);
                if (float.TryParse(result.Substring(end1 + 1, end2 - end1 - 1), out float f10))
                {
                    host.Load_10 = f10;
                }

                int end3 = result.IndexOf(' ', end2 + 2);
                if (float.TryParse(result.Substring(end2 + 2, end3 - end2 - 2), out float f15))
                {
                    host.Load_15 = f15;
                }
            }

            index = result.IndexOf("%ni,", StringComparison.Ordinal);
            if (index != -1)
            {
                int end = result.IndexOf("%id", index, StringComparison.Ordinal);
                if (float.TryParse(result.Substring(index + 4, end - index - 4), out float f))
                {
                    host.CPU_Used = 100.0f - f;
                }
            }

            index = result.IndexOf("Mem:", StringComparison.Ordinal);
            if (index != -1)
            {
                int end = result.IndexOf("k total,", index, StringComparison.Ordinal);
                if (uint.TryParse(result.Substring(index + 4, end - index - 4), out uint t))
                {
                    host.Mem_Total = t;
                }
                index = end;
                end = result.IndexOf("k used", index, StringComparison.Ordinal);
                if (uint.TryParse(result.Substring(index + 8, end - index - 8), out uint u))
                {
                    host.Mem_Used = u;
                }
            }
        }

        protected override void FillPid(SshClient client, Service service)
        {
            string result = client.RunCommand(
                string.Format("ps -aux|grep {0} |awk -- '{{print $2,$3}}'", service.Name)).Execute();

            string[] rows = result.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> pids = rows.Select(row => row.Split(" ", StringSplitOptions.RemoveEmptyEntries)).Where(columns => columns.Length >= 2).ToDictionary(columns => columns[0], columns => columns[1]);

            result = client.RunCommand(
                string.Format("service {0} status", service.Name)).Execute();

            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length != 0)
            {
                foreach(var pid in pids)
                {
                    if (rows[0].IndexOf(pid.Key, StringComparison.Ordinal) != -1)
                    {
                        if (int.TryParse(pid.Key, out int p))
                        {
                            service.Pid = p;
                        }
             
                        if(float.TryParse(pid.Value,out float u))
                        {
                            service.CPU_Used = u;
                        }
                        break;
                    }
                }
                service.Status = rows[0].IndexOf("running", StringComparison.Ordinal) == -1 ?  (byte)ServiceStatus.Stopped: (byte)ServiceStatus.Running;
            }

            result = client.RunCommand(
                string.Format("chkconfig --list | grep {0}", service.Name)).Execute();

            rows = result.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length!=0)
            {
                service.Config = rows[0].IndexOf("on", StringComparison.Ordinal) ==-1 ? (byte)ServiceConfig.Demand : (byte)ServiceConfig.Auto;
            }
        }

        protected override string ControlServiceCore(SshClient client, ControlService_Request request)
        {
            string cmd;
            if (request.Op == (byte)ServiceConfig.Start)
            {
                cmd=string.Format("service {0} start", request.Name);
            }
            else if (request.Op == (byte)ServiceConfig.Stop)
            {
                cmd = string.Format("service {0} stop", request.Name);
            }
            else if(request.Op==(byte)ServiceConfig.Restart)
            {
                cmd = string.Format("service {0} restart", request.Name);
            }
            else if (request.Op == (byte) ServiceConfig.Auto)
            {
                cmd = string.Format("chkconfig {0} on", request.Name);
            }
            else if (request.Op == (byte) ServiceConfig.Demand)
            {
                cmd = string.Format("chkconfig {0} off", request.Name);
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
