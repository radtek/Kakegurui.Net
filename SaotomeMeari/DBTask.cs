using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Kakegurui.Core;
using Kakegurui.Protocol;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SaotomeMeari
{
    /// <summary>
    /// 网络监测
    /// </summary>
    public class DBTask:TaskObject
    {
        /// <summary>
        /// 读取数据库间隔(秒)
        /// </summary>
        private int _dbSpan;

        /// <summary>
        /// 当前读取数据库轮询序号
        /// </summary>
        private int _dbIndex;

        /// <summary>
        /// 在集群中的序号
        /// </summary>
        private int _clusterIndex;

        /// <summary>
        /// 集群节点数量
        /// </summary>
        private int _clusterCount;

        /// <summary>
        /// 连接到的服务地址
        /// </summary>
        private IPEndPoint _serviceEndPoint;

        /// <summary>
        /// 监测线程集合
        /// </summary>
        private readonly List<PingTask> _channels=new List<PingTask>();

        private readonly ProtocolMaid _protocolMaid = new ProtocolMaid();

        private ClusterWatcher _watcher;

        public DBTask() 
            : base("db task")
        {
        }


        /// <summary>
        /// 通知设备状态事件处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoticingStatusEventHandler(object sender, NoticingStatusEventArgs e)
        {
            _protocolMaid.SendTcp(_serviceEndPoint, ProtocolPacker.Request(NoticingStatusEventArgs.Id,e).Item1);
        }


        private void ClusterNodeChangedHandler(object sender, ClusterNodeChangedEventArgs e)
        {
            _clusterIndex = e.Index;
            _clusterCount = e.Count;
            LogPool.Logger.LogInformation("cluster count {0} index {1}",e.Count,e.Index);
        }

        protected override void ActionCore()
        {
            _dbSpan = AppConfig.ReadInt32("dbspan") ?? 60;
            LogPool.Logger.LogInformation("database span={0}", _dbSpan);
            _dbIndex = 0;

            _clusterIndex = 0;
            string address = AppConfig.ReadString("zookeeper");
            if (address == null)
            {
                _clusterCount = 1;
                LogPool.Logger.LogInformation("not found zk count 1 index 0");
            }
            else
            {
                _clusterCount = 0;
                _watcher = new ClusterWatcher(address);
                _watcher.ClusterNodeChanged += ClusterNodeChangedHandler;
            }

            int channelCount = AppConfig.ReadInt32("channelcount") ?? 0;
            LogPool.Logger.LogInformation("channel count={0}", channelCount);
            for (int i = 0; i < channelCount; ++i)
            {
                PingTask channel = new PingTask(i + 1);
                channel.NoticingStatus += NoticingStatusEventHandler;
                channel.Start();
                _channels.Add(channel);
            }

            string serviceIp = AppConfig.ReadString("serviceip");
            int servicePort = AppConfig.ReadInt32("serviceport") ?? 0;
            LogPool.Logger.LogInformation("service address={0}:{1}", serviceIp, servicePort);
            if (IPAddress.TryParse(serviceIp, out IPAddress ip))
            {
                _serviceEndPoint = new IPEndPoint(ip, servicePort);
                _protocolMaid.AddConnectEndPoint(_serviceEndPoint, new ProtocolHandler());
            }
            _protocolMaid.Start();
            while (!IsCancelled())
            {
                if (_channels.Count == 0)
                {
                    return;
                }
                if (_dbIndex % _dbSpan == 0 && _clusterCount != 0)
                {
                    Dictionary<uint, IPStatus> ips = new Dictionary<uint, IPStatus>();
                    using (MySqlConnection con = new MySqlConnection(AppConfig.ReadString("mysql")))
                    {
                        try
                        {
                            con.Open();
                            MySqlCommand cmd1 = new MySqlCommand("Select Count(*) From t_oms_device", con);

                            int totalCount = Convert.ToInt32(cmd1.ExecuteScalar());
                            int selectCount = Convert.ToInt32(Math.Ceiling(totalCount / Convert.ToDouble(_clusterCount)));

                            MySqlCommand cmd2 = new MySqlCommand(
                                string.Format("Select Ipdz,Sbzt From t_oms_device limit {0},{1}", _clusterIndex * selectCount, selectCount), con);

                            MySqlDataReader reader = cmd2.ExecuteReader();
                            while (reader.Read())
                            {
                                string value1 = reader[0].ToString();
                                string value2 = reader[1].ToString();
                                if (IPAddress.TryParse(value1, out IPAddress i) &&
                                    int.TryParse(value2, out int status)
                                )
                                {
                                    ips[BitConverter.ToUInt32(i.GetAddressBytes(), 0)] =
                                        status == 1 ? IPStatus.Success : IPStatus.TimedOut;
                                }
                            }
                            LogPool.Logger.LogInformation("query device success {0}", ips.Count);
                        }
                        catch (MySqlException e)
                        {
                            LogPool.Logger.LogError(e, "query device failed");
                        }
                        finally
                        {
                            con.Close();
                        }
                    }


                    foreach (PingTask channel in _channels)
                    {
                        channel.Clear();
                    }
                    int index = 0;

                    foreach (var i in ips)
                    {
                        if (index >= _channels.Count)
                        {
                            index = 0;
                        }
                        _channels[index].Add(i);
                        ++index;
                    }
                }
                ++_dbIndex;
                Thread.Sleep(1000);
            }
       
            foreach (PingTask channel in _channels)
            {
                channel.Stop();
            }
            _protocolMaid.RemoveConnectEndPoint(_serviceEndPoint);
            _protocolMaid.Stop();
        }
    }
}
