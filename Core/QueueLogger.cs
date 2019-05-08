using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志队列
    /// </summary>
    public class QueueLogger:Logger
    {
        /// <summary>
        /// 队列中最多保存日志的数量
        /// </summary>
        private readonly int _maxCount;

        /// <summary>
        /// 日志队列
        /// </summary>
        public ConcurrentQueue<string> Queue { get; } = new ConcurrentQueue<string>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="level">日志级别</param>
        public QueueLogger(LogLevel level) 
            : base(level, level)
        {
            _maxCount = AppConfig.QueueMaxCount;
        }

        protected override void LogCore(string log)
        {
            if (Queue.Count >= _maxCount)
            {
                Queue.TryDequeue(out string str);
            }
            Queue.Enqueue(log);
        }
    }
}
