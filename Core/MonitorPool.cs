using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// 可以获取状态的任务
    /// </summary>
    public interface IStatusJob
    {
        MonitorStatusItem GetStatus();
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
    /// 定时任务
    /// </summary>
    public class FixedJobTask : TaskObject
    {
        /// <summary>
        /// 定时任务集合
        /// </summary>
        public ConcurrentDictionary<IFixedJob, FixedJobItem> FixedJobs { get; } = new ConcurrentDictionary<IFixedJob, FixedJobItem>();

        public FixedJobTask()
            : base("fixed job task")
        {
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="job">定时任务</param>
        /// <param name="level">时间间隔级别</param>
        /// <param name="span">时间偏移</param>
        /// <param name="name">任务名称</param>
        public void AddFixedJob(IFixedJob job, DateTimeLevel level, TimeSpan span, string name)
        {
            DateTime nextTime = TimePointConvert
                .NextTimePoint(level, TimePointConvert.CurrentTimePoint(level, DateTime.Now));
            LogPool.Logger.LogInformation("add fixed job {0} {1} {2}", name, level, nextTime.Add(span).ToString("yyyy-MM-dd HH:mm:ss"));
            FixedJobs.TryAdd(job, new FixedJobItem
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
            if (FixedJobs.TryRemove(job, out FixedJobItem item))
            {
                LogPool.Logger.LogInformation("remote fixed job {0} {1} {2}", item.Name, item.Level, item.Time.Add(item.Span).ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                DateTime now = DateTime.Now;
                foreach (var pair in FixedJobs)
                {
                    if (now > pair.Value.Time.Add(pair.Value.Span))
                    {
                        LogPool.Logger.LogInformation("start fixed job {0} {1} {2}", pair.Value.Name, pair.Value.Level, pair.Value.Time.Add(pair.Value.Span).ToString("yyyy-MM-dd HH:mm:ss"));
                        DateTime nextTime = TimePointConvert.NextTimePoint(pair.Value.Level, pair.Value.Time);
                        pair.Key.Handle(pair.Value.Time, nextTime);
                        pair.Value.Time = nextTime;
                        LogPool.Logger.LogInformation("next fixed job {0} {1} {2}", pair.Value.Name, pair.Value.Level, pair.Value.Time.Add(pair.Value.Span).ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }

    public class MonitorStatusItem
    {
        public string Name { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// 监控状态
    /// </summary>
    public class MonitorStatus
    {
        /// <summary>
        /// cpu
        /// </summary>
        public string Cpu { get; set; }
        /// <summary>
        /// 内存(mb)
        /// </summary>
        public string Memory { get; set; }
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadCount { get; set; }
        /// <summary>
        /// 监控信息集合
        /// </summary>
        public List<MonitorStatusItem> Status { get; set; }
        /// <summary>
        /// 定时任务集合
        /// </summary>
        public List<string> FixedJobs { get; set; }
        /// <summary>
        /// 警告日志集合
        /// </summary>
        public List<string> WarningLogs { get; set; }
        /// <summary>
        /// 错误日志集合
        /// </summary>
        public List<string> ErrorLogs { get; set; }
    }
    /// <summary>
    /// 监控池
    /// </summary>
    public static class MonitorPool
    {
        /// <summary>
        /// 计时器
        /// </summary>
        private static readonly System.Timers.Timer _timer = new System.Timers.Timer();

        /// <summary>
        /// 定时任务
        /// </summary>
        private static readonly FixedJobTask _fixedJobTask = new FixedJobTask();

        /// <summary>
        /// 轮询监控任务集合
        /// </summary>
        private static readonly ConcurrentDictionary<IStatusJob,object> _pollJbos = new ConcurrentDictionary<IStatusJob, object>();

        /// <summary>
        /// 可重启的任务集合
        /// </summary>
        private static readonly ConcurrentDictionary<IRestartableJob, object> _restartableJobs = new ConcurrentDictionary<IRestartableJob, object>();
       
        /// <summary>
        /// 上一次进程cpu的时间总和
        /// </summary>
        private static TimeSpan _lastCpuTime;

        /// <summary>
        /// 上一次的状态
        /// </summary>
        private static string _cpu;

        /// <summary>
        /// 构造函数
        /// </summary>
        static MonitorPool()
        {
            _timer.Interval = 1000;
            _timer.Elapsed += ElapsedEventHandler;
            _timer.Start();
            _fixedJobTask.Start();
        }

        /// <summary>
        /// 添加可重启的任务
        /// </summary>
        /// <param name="job">可重启的任务</param>
        public static void AddRestartableJob(IRestartableJob job)
        {
            _restartableJobs.TryAdd(job, null);
        }

        /// <summary>
        /// 删除可重启的任务
        /// </summary>
        /// <param name="job">可重启的任务</param>
        public static void RemoveRestartableJob(IRestartableJob job)
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
        public static void AddFixedJob(IFixedJob job,DateTimeLevel level,TimeSpan span, string name)
        {
            _fixedJobTask.AddFixedJob(job,level,span,name);
        }

        /// <summary>
        ///移除定时任务
        /// </summary>
        /// <param name="job">定时任务</param>
        public static void RemoteFixedJob(IFixedJob job)
        {
            _fixedJobTask.RemoteFixedJob(job);
        }

        /// <summary>
        /// 添加轮询监控任务
        /// </summary>
        /// <param name="statusJob">轮询监控任务</param>
        public static void AddPollJob(IStatusJob statusJob)
        {
            _pollJbos.TryAdd(statusJob,null);
        }

        /// <summary>
        /// 移除轮询监控任务
        /// </summary>
        /// <param name="statusJob">轮询监控任务</param>
        public static void RemovePollJob(IStatusJob statusJob)
        {
            _pollJbos.TryRemove(statusJob, out object obj);
        }

        /// <summary>
        /// 查询状态
        /// </summary>
        /// <returns>状态描述</returns>
        public static MonitorStatus GetStatus()
        {
            Process process = Process.GetCurrentProcess();
            MonitorStatus status = new MonitorStatus
            {
                Cpu = _cpu,
                Memory = $"{process.WorkingSet64 / 1024.0 / 1024.0:N2}",
                ThreadCount = process.Threads.Count,
                Status = new List<MonitorStatusItem>()
            };
            foreach (var pair in _pollJbos)
            {
                status.Status.Add(pair.Key.GetStatus());
            }
            status.FixedJobs = new List<string>();
            foreach (var pair in _fixedJobTask.FixedJobs)
            {
                status.FixedJobs.Add($"{pair.Value.Name} {pair.Value.Level} {pair.Value.Time.Add(pair.Value.Span):yyyy-MM-dd HH:mm:ss}");
            }
            status.WarningLogs = new List<string>(LogPool.Warnings);
            status.ErrorLogs = new List<string>(LogPool.Errors);
            return status;
        }

        /// <summary>
        /// 重启
        /// </summary>
        public static void Restart()
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
        private static void ElapsedEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            Process process = Process.GetCurrentProcess();
            TimeSpan currentCpuTime = process.TotalProcessorTime;
            _cpu =  $"{(currentCpuTime - _lastCpuTime).TotalMilliseconds / _timer.Interval / Environment.ProcessorCount * 100:N2}";
            _lastCpuTime = currentCpuTime;
        }
    }
}
