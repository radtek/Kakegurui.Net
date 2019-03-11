using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Kakegurui.Core;
using Kakegurui.Net;
using Kakegurui.Protocol;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace JabamiYumeko
{
    /// <summary>
    /// 系统监控
    /// </summary>
    public class MonitorTask : TaskObject
    {
        /// <summary>
        /// 检测线程集合
        /// </summary>
        private readonly ConcurrentDictionary<string, PerformanceTask> _channels = new ConcurrentDictionary<string, PerformanceTask>();

        /// <summary>
        /// 监听地址
        /// </summary>
        private static int _servicePort;

        /// <summary>
        /// 处理实例
        /// </summary>
        private readonly ProtocolHandler _handler = new ProtocolHandler();

        /// <summary>
        /// 协议收发
        /// </summary>
        private readonly ProtocolMaid _protocolMaid = new ProtocolMaid();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonitorTask()
            :base("monitor")
        {
            _handler.GotProtocol += GotMonitorProtocolEventHandler;
            _protocolMaid.AddTask(this);
        }

        /// <summary>
        /// 收到系统检测协议事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GotMonitorProtocolEventHandler(object sender,GotProtocolEventArgs e)
        {
            if (e.ProtocolId == ControlService_Request.Id)
            {
                ControlService_Request config = new ControlService_Request();
                ByteFormatter.Deserialize(config, e.Buffer, e.Offset+ProtocolHead.HeadSize);
                ControlService_Response response = new ControlService_Response
                {
                    Result = _channels.TryGetValue(config.Ip, out PerformanceTask channel)
                        ? channel.ControlService(config)
                        : PerformanceTask.UnknownIp
                };
                e.ResponseBuffer = ProtocolPacker.Response(ControlService_Response.Id, e.TimeStamp, response);
            }
        }

        /// <summary>
        /// 获取主机快照事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GotHostSnapshotHandler(object sender, Host e)
        {
            _protocolMaid.Notice(ProtocolPacker.Request(Host.Id, e).Item1);   
        }

        /// <summary>
        /// 获取服务快照事件执行函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GotServiceSnapshotHandler(object sender, Service e)
        {
            _protocolMaid.Notice(ProtocolPacker.Request(Service.Id, e).Item1);
        }

        protected override void ActionCore()
        {
            int dbIndex = 0;
            int dbSpan = AppConfig.ReadInt32("dbspan") ?? 60;

            _servicePort = AppConfig.ReadInt32("serviceport") ?? 0;
            LogPool.Logger.LogInformation("service port={0}", _servicePort);
            _protocolMaid.AddListenEndPoint(_servicePort, _handler);
            _protocolMaid.Start();
            while (!IsCancelled())
            {
                if (dbIndex % dbSpan == 0)
                {
                    using (MySqlConnection con = new MySqlConnection(AppConfig.ReadString("mysql")))
                    {
                        try
                        {
                            con.Open();
                            MySqlCommand cmd = new MySqlCommand("Select Ip,Port,UserName,Password,OS From t_oms_host", con);
                            MySqlDataReader reader = cmd.ExecuteReader();
                            Dictionary<string, Host> hosts = new Dictionary<string, Host>();

                            while (reader.Read())
                            {
                                string ip = reader[0].ToString();
                                ushort port = Convert.ToUInt16(reader[1]);
                                string userName = reader[2].ToString();
                                string password = reader[3].ToString();
                                int os = Convert.ToInt32(reader[4]);
                                if (Enum.IsDefined(typeof(OperationSystem), os))
                                {
                                    hosts.Add(ip, new Host
                                    {
                                        Ip = ip,
                                        Port = port,
                                        UserName = userName,
                                        Password = password,
                                        OS = (OperationSystem)os
                                    });
                                }
                                else
                                {
                                    LogPool.Logger.LogInformation("unknown os {0} {1}", ip, os);
                                }
                            }
                            reader.Close();
                            LogPool.Logger.LogInformation("query {0} hosts", hosts.Count);
                            foreach (var channel in _channels.ToArray())
                            {
                                if (!hosts.ContainsKey(channel.Key))
                                {
                                    if (_channels.TryRemove(channel.Key, out PerformanceTask c))
                                    {
                                        _protocolMaid.RemoveTask(c);
                                        c.Stop();
                                    }
                                }
                            }

                            foreach (var ip in hosts)
                            {
                                if (!_channels.ContainsKey(ip.Key))
                                {
                                    PerformanceTask channel;
                                    if (ip.Value.OS == OperationSystem.CentOS6)
                                    {
                                        channel = new CentOS6(ip.Value);
                                    }
                                    else if (ip.Value.OS == OperationSystem.CentOS7)
                                    {
                                        channel = new CentOS7(ip.Value);
                                    }
                                    else
                                    {
                                        channel = new Windows2008(ip.Value);
                                    }
                                    channel.GotHostSnapshot += GotHostSnapshotHandler;
                                    channel.GotServiceSnapshot += GotServiceSnapshotHandler;
                                    _channels[ip.Key] = channel;
                                    _protocolMaid.AddTask(channel);
                                    channel.Start();
                                }
                            }

                            foreach (var channel in _channels)
                            {
                                MySqlCommand cmd1 = new MySqlCommand(
                                    string.Format("Select Ip,Name From t_oms_service Where Ip='{0}'", channel.Key), con);
                                MySqlDataReader reader1 = cmd1.ExecuteReader();
                                List<Service> services = new List<Service>();
                                while (reader1.Read())
                                {
                                    services.Add(new Service
                                    {
                                        Ip = reader1.GetString("Ip"),
                                        Name = reader1.GetString("Name")
                                    });
                                }
                                reader1.Close();
                                channel.Value.AssignServices(services);
                            }
                        }
                        catch (MySqlException e)
                        {
                            LogPool.Logger.LogInformation(e, "mysql");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
                Thread.Sleep(1000);
                dbIndex++;
            }
            _protocolMaid.Stop();
        }
    }
}
