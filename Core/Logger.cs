using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志类
    /// </summary>
    public abstract class Logger:ILoggerProvider,ILogger
    {
        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 日志筛选最低级别
        /// </summary>
        private readonly LogLevel _minLevel;

        /// <summary>
        /// 日志筛选最高级别
        /// </summary>
        private readonly LogLevel _maxLevel;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="minLevel">日志筛选最低级别</param>
        /// <param name="maxLevel">日志筛选最高级别</param>
        protected Logger(LogLevel minLevel, LogLevel maxLevel)
        {
            _minLevel = minLevel;
            _maxLevel = maxLevel;
        }

        /// <summary>
        /// 供子类实现的写日志
        /// </summary>
        /// <param name="log">日志内容</param>
        protected abstract void LogCore(string log);

        #region 实现ILoggerProvider,ILogger
        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                lock (_lockObj)
                {
                    try
                    {
                        LogCore(exception == null
                            ? $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{logLevel}][{Thread.CurrentThread.ManagedThreadId}] {state}"
                            : $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{logLevel}][{Thread.CurrentThread.ManagedThreadId}] {state}\n{exception}");
                    }
                    catch
                    {
                    }
                }               
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _minLevel && logLevel <= _maxLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }
        #endregion
    }
}
