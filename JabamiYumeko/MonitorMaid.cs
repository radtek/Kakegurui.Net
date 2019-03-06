using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
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
    public class MonitorMaid:ProtocolMaid
    {
        /// <summary>
        /// 读取数据库轮询序号
        /// </summary>
        private int _dbIndex;

        /// <summary>
        /// 读取数据库时间间隔
        /// </summary>
        private int _dbSpan;

        /// <summary>
        /// 检测线程集合
        /// </summary>
        private readonly ConcurrentDictionary<string, PerformanceChannel> _channels = new ConcurrentDictionary<string, PerformanceChannel>();

        /// <summary>
        /// 监听地址
        /// </summary>
        private static IPEndPoint _serviceEndPoint;

        /// <summary>
        /// 处理实例
        /// </summary>
        private readonly ProtocolHandler _handler = new ProtocolHandler();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonitorMaid()
        {
            _handler.GotProtocol += GotMonitorProtocolEventHandler;
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
                    Result = _channels.TryGetValue(config.Ip, out PerformanceChannel channel)
                        ? channel.ControlService(config)
                        : PerformanceChannel.UnknownIp
                };
                e.ResponseBuffer = ProtocolPacker.Response(ControlService_Response.Id, e.TimeStamp, response);
            }
        }

        protected override void InitCore()
        {
            _dbIndex = 0;
            _dbSpan = AppConfig.ReadInt32("dbspan")??60;

            int servicePort = AppConfig.ReadInt32("serviceport") ?? 0;
            LogPool.Logger.LogInformation("service port={0}",servicePort);
            _serviceEndPoint = new IPEndPoint(IPAddress.Any, servicePort);
            AddListenEndPoint(_serviceEndPoint, _handler);
        }

        protected override void PollCore()
        {
            if (_dbIndex % _dbSpan == 0)
            {
                using (MySqlConnection con = new MySqlConnection(AppConfig.ReadString("mysql")))
                {
                    try
                    {
                        con.Open();
                        MySqlCommand cmd = new MySqlCommand("Select Ip,Port,UserName,Password,OS From t_oms_host", con);
                        MySqlDataReader reader = cmd.ExecuteReader();
                        Dictionary<string,Host> hosts = new Dictionary<string, Host>();
                       
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
                                LogPool.Logger.LogInformation("unknown os {0} {1}",ip,os);
                            }
                        }
                        reader.Close();
                        LogPool.Logger.LogInformation("query {0} hosts", hosts.Count);
                        foreach (var channel in _channels.ToArray())
                        {
                            if (!hosts.ContainsKey(channel.Key))
                            {
                                if (_channels.TryRemove(channel.Key, out PerformanceChannel c))
                                {
                                    c.Stop();
                                }                               
                            }
                        }

                        foreach (var ip in hosts)
                        {
                            if (!_channels.ContainsKey(ip.Key))
                            {
                                PerformanceChannel channel;
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
                                _channels[ip.Key]=channel;
                                channel.Start();
                            }
                        }

                        foreach (var channel in _channels)
                        {
                            MySqlCommand cmd1 = new MySqlCommand(
                                string.Format("Select Ip,Name From t_oms_service Where Ip='{0}'",channel.Key), con);
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
                    catch (Exception e)
                    {
                        LogPool.Logger.LogInformation(e,"mysql");
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            _dbIndex++;
        }

        protected override void ExitCore()
        {
            RemoveConnectEndPoint(_serviceEndPoint);
        }

        private void GotHostSnapshotHandler(object sender, GotHostSnapshotEventArgs e)
        {
            foreach (var p in _sockets)
            {
                if (p.Value.Type == SocketType.Accept)
                {
                    SendTcp(p.Key, ProtocolPacker.Request(Host.Id, e.Host).Item1);
                }
            }
          
        }

        private void GotServiceSnapshotHandler(object sender, GotServiceSnapshotEventArgs e)
        {
            foreach (var p in _sockets)
            {
                if (p.Value.Type == SocketType.Accept)
                {
                    SendTcp(p.Key, ProtocolPacker.Request(Service.Id, e.Service).Item1);
                }
            }
        }
    }
}
