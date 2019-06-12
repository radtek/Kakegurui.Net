using System;
using System.Collections.Concurrent;
using System.Threading;
using Kakegurui.Core;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Monitor
{
    /// <summary>
    /// 定时任务接口
    /// </summary>
    public interface IFixedJob
    {
        /// <summary>
        /// 执行定时任务
        /// </summary>
        /// <param name="currentTime">当前时间点</param>
        /// <param name="nextTime">下个时间点</param>
        void Handle(DateTime currentTime, DateTime nextTime);
    }

    /// <summary>
    /// 定时任务信息
    /// </summary>
    public class FixedJobItem
    {
        public string Name { get; set; }
        public DateTimeLevel Level { get; set; }
        public DateTime CurrentTime { get; set; }
        public DateTime ChangeTime { get; set; }
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

        /// <summary>
        /// 构造函数
        /// </summary>
        public FixedJobTask()
            : base("fixed job task")
        {
            
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="fixedJob">定时任务</param>
        /// <param name="level">时间间隔级别</param>
        /// <param name="span">时间偏移</param>
        /// <param name="name">任务名称</param>
        public void AddFixedJob(IFixedJob fixedJob, DateTimeLevel level, TimeSpan span, string name)
        {
            DateTime currentTime = TimePointConvert.CurrentTimePoint(level, DateTime.Now);
            DateTime changeTime = TimePointConvert.NextTimePoint(level, currentTime).Add(span);
            LogPool.Logger.LogInformation("add fixed job {0} {1} {2}", name, level, changeTime.ToString("yyyy-MM-dd HH:mm:ss"));
            FixedJobs.TryAdd(fixedJob, new FixedJobItem
            {
                Name = name,
                Level = level,
                Span = span,
                CurrentTime = currentTime,
                ChangeTime = changeTime
            });
        }

        /// <summary>
        ///移除定时任务
        /// </summary>
        /// <param name="fixedJob">定时任务</param>
        public void RemoteFixedJob(IFixedJob fixedJob)
        {
            if (FixedJobs.TryRemove(fixedJob, out FixedJobItem item))
            {
                LogPool.Logger.LogInformation("remote fixed job {0} {1}", item.Name, item.Level);
            }
        }

        protected override void ActionCore()
        {
            while (!IsCancelled())
            {
                DateTime now = DateTime.Now;
                foreach (var pair in FixedJobs)
                {
                    if (now > pair.Value.ChangeTime)
                    {
                        LogPool.Logger.LogDebug("start fixed job {0} {1} {2}", pair.Value.Name, pair.Value.Level, pair.Value.ChangeTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        DateTime nextTime = TimePointConvert.NextTimePoint(pair.Value.Level, pair.Value.CurrentTime);
                        try
                        {
                            pair.Key.Handle(pair.Value.CurrentTime, nextTime);
                        }
                        catch (Exception ex)
                        {
                            LogPool.Logger.LogError("fixed job", ex);
                        }
                        pair.Value.CurrentTime = nextTime;
                        pair.Value.ChangeTime = TimePointConvert.NextTimePoint(pair.Value.Level, nextTime).Add(pair.Value.Span);
                        LogPool.Logger.LogDebug("next fixed job {0} {1} {2}", pair.Value.Name, pair.Value.Level, pair.Value.ChangeTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}
