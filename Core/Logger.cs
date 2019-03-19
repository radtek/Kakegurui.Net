using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志筛选接口
    /// </summary>
    public interface ILogFilter
    {
        bool Filter(LogLevel level);
    }

    /// <summary>
    /// 任意级别
    /// </summary>
    public class AllFilter : ILogFilter
    {
        public bool Filter(LogLevel level)
        {
            return true;
        }
    }

    /// <summary>
    /// 等于
    /// </summary>
    public class EqualFilter : ILogFilter
    {
        private readonly LogLevel _level;

        public EqualFilter(LogLevel level)
        {
            _level = level;
        }
        public bool Filter(LogLevel level)
        {
            return level == _level;
        }
    }

    /// <summary>
    /// 不等于
    /// </summary>
    public class NotEqualFilter : ILogFilter
    {
        private readonly LogLevel _level;

        public NotEqualFilter(LogLevel level)
        {
            _level = level;
        }
        public bool Filter(LogLevel level)
        {
            return level != _level;
        }
    }

    /// <summary>
    /// 大于等于
    /// </summary>
    public class HigherEqualFilter : ILogFilter
    {
        private readonly LogLevel _level;

        public HigherEqualFilter(LogLevel level)
        {
            _level = level;
        }
        public bool Filter(LogLevel level)
        {
            return level >= _level;
        }
    }

    /// <summary>
    /// 大于
    /// </summary>
    public class HigherFilter : ILogFilter
    {
        private readonly LogLevel _level;

        public HigherFilter(LogLevel level)
        {
            _level = level;
        }
        public bool Filter(LogLevel level)
        {
            return level > _level;
        }
    }

    /// <summary>
    /// 范围
    /// </summary>
    public class RangeFilter : ILogFilter
    {
        private readonly LogLevel _minLevel;
        private readonly LogLevel _maxLevel;

        public RangeFilter(LogLevel minLevel,LogLevel maxLevel)
        {
            _minLevel = minLevel;
            _maxLevel = maxLevel;
        }
        public bool Filter(LogLevel level)
        {
            return level >= _minLevel && level<= _maxLevel;
        }
    }

    /// <summary>
    /// 日志类
    /// </summary>
    public abstract class Logger:ILoggerProvider,ILogger
    {
        private readonly ILogFilter _filter;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filter">日志筛选接口</param>
        protected Logger(ILogFilter filter)
        {
            _filter = filter;
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
                lock (this)
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
            return _filter != null && _filter.Filter(logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }
        #endregion
    }
}
