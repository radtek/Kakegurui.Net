using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;
using Renci.SshNet;

namespace JabamiYumeko
{
    /// <summary>
    /// 系统检测线程
    /// </summary>
    public abstract class PerformanceTask:TaskObject
    {
        /// <summary>
        /// 命令执行成功
        /// </summary>
        public const string Success="success";
     
        /// <summary>
        /// 未知ip
        /// </summary>
        public const string UnknownIp= "unknown ip";
    
        /// <summary>
        /// 未知系统
        /// </summary>
        public const string UnknownOS="unknown os";
     
        /// <summary>
        /// 未知操作
        /// </summary>
        public const string UnknownOP="unknown op";

        /// <summary>
        /// 读取快照间隔
        /// </summary>
        private int _snapshotSpan;

        /// <summary>
        /// 当前线程的主机信息
        /// </summary>
        private readonly Host _host;

        /// <summary>
        /// 服务集合
        /// </summary>
        private readonly ConcurrentBag<Service> _services = new ConcurrentBag<Service>();

        /// <summary>
        /// 获取主机快照事件
        /// </summary>
        public event EventHandler<GotHostSnapshotEventArgs> GotHostSnapshot;

        /// <summary>
        /// 获取服务快照事件
        /// </summary>
        public event EventHandler<GotServiceSnapshotEventArgs> GotServiceSnapshot;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="host">主机信息</param>
        protected PerformanceTask(Host host) : base("performance_"+ host.Ip)
        {
            _host = host;
        }

        /// <summary>
        /// 设置主机下的服务集合
        /// </summary>
        /// <param name="services"></param>
        public void AssignServices(List<Service> services)
        {
            _services.Clear();
            foreach (Service service in services)
            {
                _services.Add(service);
            }
        }

        /// <summary>
        /// 控制服务
        /// </summary>
        /// <param name="request">服务控制参数</param>
        /// <returns>操作结果</returns>
        public string ControlService(ControlService_Request request)
        {
            using (SshClient client = new SshClient(_host.Ip, _host.Port, _host.UserName, _host.Password))
            {
                try
                {
                    client.Connect();
                    return ControlServiceCore(client, request);
                }
                catch (SocketException e)
                {
                    LogPool.Logger.LogInformation(e, "ssh control {0}:{1} {2} {3}",_host.Ip,_host.Port,_host.UserName,_host.Password);
                    return e.Message;
                }
                finally
                {
                    client.Disconnect();
                }
            }
        }

        /// <summary>
        /// 供子类实现的控制服务函数
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="request">控制参数</param>
        /// <returns>操作结果</returns>
        protected abstract string ControlServiceCore(SshClient client, ControlService_Request request);

        /// <summary>
        /// 供子类实现的获取主机快照函数
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="host">用于设置主机快照信息的实例</param>
        protected abstract void FillHostCore(SshClient client, Host host);

        /// <summary>
        /// 供子类实现的获取服务快照函数
        /// </summary>
        /// <param name="client">ssh</param>
        /// <param name="service">用于设置服务快照信息的实例</param>
        protected abstract void FillServiceCore(SshClient client, Service service);

        protected override void ActionCore()
        {
            int _pollIndex=0;
            _snapshotSpan = AppConfig.ReadInt32("snapshotspan") ?? 60;
            while (!IsCancelled())
            {
                if (_pollIndex % _snapshotSpan == 0)
                {  
                    using (SshClient client = new SshClient(_host.Ip, _host.Port, _host.UserName, _host.Password))
                    {
                        try
                        {
                            client.Connect();
                            Host host = new Host();
                            FillHostCore(client, host);
                            host.Ip = _host.Ip;
                            host.Status = (byte)HostStatus.Connection;
                            host.TimeStamp = TimeStampConvert.ToTimeStamp();
                            
                            GotHostSnapshot?.Invoke(this, new GotHostSnapshotEventArgs
                            {
                                Protocol = host
                            });

                            foreach (Service service in _services)
                            {
                                FillServiceCore(client, service);
                                service.TimeStamp = TimeStampConvert.ToTimeStamp();
                                GotServiceSnapshot?.Invoke(this, new GotServiceSnapshotEventArgs
                                {
                                    Protocol = service
                                });
                            }

                        }
                        catch (SocketException e)
                        {
                            LogPool.Logger.LogInformation(e, "ssh error {0}:{1} {2} {3}", _host.Ip, _host.Port, _host.UserName, _host.Password);
                        }
                        finally
                        {
                            client.Disconnect();
                        }
                    }
                }
                _pollIndex++;
                Thread.Sleep(1000);
            }
        }
    }
}
