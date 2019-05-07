using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 可重启的任务
    /// </summary>
    public interface IRestartableJob
    {
        void Restart();
    }

    /// <summary>
    /// 可查询状态的任务
    /// </summary>
    public interface IStatusJob
    {
        string GetStatus();
    }

    /// <summary>
    /// 轮询监控任务
    /// </summary>
    public interface IPollJob
    {
        string GetMonitor();
    }

    /// <summary>
    /// 定时任务
    /// </summary>
    public interface IFixedJob
    {
        void Handle(DateTime currentTime, DateTime nextTime);
    }

    /// <summary>
    /// 定时任务信息
    /// </summary>
    public class FixedJobItem
    {
        public string Name { get; set; }
        public DateTimeLevel Level { get; set; }
        public DateTime Time { get; set; }
        public TimeSpan Span { get; set; }
    }

    /// <summary>
    /// 监控池
    /// </summary>
    public class MonitorPool:TaskObject
    {
        /// <summary>
        /// 计时器
        /// </summary>
        private readonly System.Timers.Timer _timer = new System.Timers.Timer();

        /// <summary>
        /// 定时任务集合
        /// </summary>
        private readonly ConcurrentDictionary<IFixedJob, FixedJobItem> _fixedJobs = new ConcurrentDictionary<IFixedJob, FixedJobItem>();

        /// <summary>
        /// 轮询监控任务集合
        /// </summary>
        private readonly ConcurrentDictionary<IPollJob,object> _pollJbos = new ConcurrentDictionary<IPollJob, object>();

        /// <summary>
        /// 可查询状态的任务集合
        /// </summary>
        private readonly ConcurrentDictionary<IStatusJob, object> _statusJobs = new ConcurrentDictionary<IStatusJob, object>();

        /// <summary>
        /// 可重启的任务集合
        /// </summary>
        private readonly ConcurrentDictionary<IRestartableJob, object> _restartableJobs = new ConcurrentDictionary<IRestartableJob, object>();
       
        /// <summary>
        /// 上一次进程cpu的时间总和
        /// </summary>
        private TimeSpan _lastCpuTime;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonitorPool()
            : base("monitor_pool")
        {
            _timer.Interval = AppConfig.MonitorSpan * 1000;
            _timer.Elapsed += ElapsedEventHandler;
        }

        /// <summary>
        /// 添加可查询状态的任务
        /// </summary>
        /// <param name="job">可查询状态的任务</param>
        public void AddStatusJob(IStatusJob job)
        {
            _statusJobs.TryAdd(job,null);
        }

        /// <summary>
        /// 移除可查询状态的任务
        /// </summary>
        /// <param name="job">可查询状态的任务</param>
        public void RemoveStatusJob(IStatusJob job)
        {
            _statusJobs.TryRemove(job, out object item);
        }

        /// <summary>
        /// 添加可重启的任务
        /// </summary>
        /// <param name="job">可重启的任务</param>
        public void AddRestartableJob(IRestartableJob job)
        {
            _restartableJobs.TryAdd(job, null);
        }

        /// <summary>
        /// 删除可重启的任务
        /// </summary>
        /// <param name="job">可重启的任务</param>
        public void RemoveRestartableJob(IRestartableJob job)
        {
            _restartableJobs.TryRemove(job, out object obj);
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="job">定时任务</param>
        /// <param name="level">时间间隔级别</param>
        /// <param name="span">时间偏移</param>
        /// <param name="name">任务名称</param>
        public void AddFixedJob(IFixedJob job,DateTimeLevel level,TimeSpan span, string name)
        {
            DateTime nextTime = TimePointConvert
                .NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, DateTime.Now)).Add(span);
            LogPool.Logger.LogInformation("add_fixedjob {0} {1} {2}", name, level, nextTime.ToString("yyyy-MM-dd HH:mm:ss"));
            _fixedJobs.TryAdd(job,new FixedJobItem
            {
                Name = name,
                Level = level,
                Time = nextTime,
                Span = span
            });
        }

        /// <summary>
        ///移除定时任务
        /// </summary>
        /// <param name="job">定时任务</param>
        public void RemoteFixedJob(IFixedJob job)
        {
            if (_fixedJobs.TryRemove(job, out FixedJobItem item))
            {
                LogPool.Logger.LogInformation("remove_fixedjob {0}", item.Name);
            }
        }

        /// <summary>
        /// 添加轮询监控任务
        /// </summary>
        /// <param name="pollJob">轮询监控任务</param>
        public void AddPollJob(IPollJob pollJob)
        {
            _pollJbos.TryAdd(pollJob,null);
        }

        /// <summary>
        /// 移除轮询监控任务
        /// </summary>
        /// <param name="pollJob">轮询监控任务</param>
        public void RemovePollJob(IPollJob pollJob)
        {
            _pollJbos.TryRemove(pollJob, out object obj);
        }

        /// <summary>
        /// 查询状态
        /// </summary>
        /// <returns>状态描述</returns>
        public string GetStatus()
        {
            StringBuilder builder = new StringBuilder();
            foreach (var pair in _statusJobs)
            {
                builder.AppendLine(pair.Key.GetStatus());
            }

            return builder.ToString();
        }

        /// <summary>
        /// 重启
        /// </summary>
        public void Restart()
        {
            foreach (var pair in _restartableJobs)
            {
                pair.Key.Restart();
            }
        }

        /// <summary>
        /// 计时器事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ElapsedEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            Process process = Process.GetCurrentProcess();
            TimeSpan currentCpuTime = process.TotalProcessorTime;
            var value = (currentCpuTime - _lastCpuTime).TotalMilliseconds / _timer.Interval / Environment.ProcessorCount * 100;
            _lastCpuTime = currentCpuTime;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"cpu:{value:N2}% memory:{process.WorkingSet64 / 1024.0 / 1024.0:N2}mb threads:{process.Threads.Count}");
            foreach (var pair in _pollJbos)
            {
                builder.AppendLine(pair.Key.GetMonitor());
            }
            LogPool.Logger.LogTrace(builder.ToString());
        }

        protected override void ActionCore()
        {
            _timer.Start();
            while (!IsCancelled())
            {
                DateTime now=DateTime.Now;
                foreach (var pair in _fixedJobs)
                {
                    if (now > pair.Value.Time)
                    {
                        DateTime nextTime = TimePointConvert.NextTimePoint(pair.Value.Level, pair.Value.Time).Add(pair.Value.Span);
                        pair.Key.Handle(pair.Value.Time,nextTime);
                        pair.Value.Time = nextTime;
                    }
                }
                Thread.Sleep(1000);
            }
            _timer.Stop();
        }
    }
}
