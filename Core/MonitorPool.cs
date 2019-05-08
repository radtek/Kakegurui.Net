using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

    public class MonitorStatus
    {
        public double Cpu { get; set; }
        public double Memory { get; set; }
        public int ThreadCount { get; set; }
        public List<string> Monitors { get; set; }
        public List<string> FixedJobs { get; set; }
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
        private static readonly ConcurrentDictionary<IPollJob,object> _pollJbos = new ConcurrentDictionary<IPollJob, object>();

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
        private static readonly MonitorStatus _lastStatus;

        /// <summary>
        /// 构造函数
        /// </summary>
        static MonitorPool()
        {
            _lastStatus=new MonitorStatus();
            _timer.Interval = AppConfig.MonitorSpan * 1000;
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
        /// <param name="pollJob">轮询监控任务</param>
        public static void AddPollJob(IPollJob pollJob)
        {
            _pollJbos.TryAdd(pollJob,null);
        }

        /// <summary>
        /// 移除轮询监控任务
        /// </summary>
        /// <param name="pollJob">轮询监控任务</param>
        public static void RemovePollJob(IPollJob pollJob)
        {
            _pollJbos.TryRemove(pollJob, out object obj);
        }

        /// <summary>
        /// 查询状态
        /// </summary>
        /// <returns>状态描述</returns>
        public static MonitorStatus GetStatus()
        {
            ElapsedEventHandler(null, null);
            return _lastStatus;
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
            _lastStatus.Cpu = (currentCpuTime - _lastCpuTime).TotalMilliseconds / _timer.Interval / Environment.ProcessorCount * 100;
            _lastCpuTime = currentCpuTime;
            _lastStatus.Memory = process.WorkingSet64 / 1024.0 / 1024.0;
            _lastStatus.ThreadCount = process.Threads.Count;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"cpu:{_lastStatus.Cpu:N2}% memory:{_lastStatus.Memory:N2}mb threads:{_lastStatus.ThreadCount}");
            _lastStatus.Monitors = new List<string>();
            foreach (var pair in _pollJbos)
            {
                string monitor = pair.Key.GetMonitor();
                _lastStatus.Monitors.Add(monitor);
                builder.AppendLine(monitor);
            }

            _lastStatus.FixedJobs = new List<string>();
            foreach (var pair in _fixedJobTask.FixedJobs)
            {
                _lastStatus.FixedJobs.Add($"{pair.Value.Name} {pair.Value.Level} {pair.Value.Time.Add(pair.Value.Span):yyyy-MM-dd HH:mm:ss}");
            }
            LogPool.Logger.LogTrace(builder.ToString());
        }

    }
}
