using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志
    /// </summary>
    public static class LogPool
    {
        /// <summary>
        /// 警告日志队列
        /// </summary>
        private static readonly QueueLogger _warningLogger = new QueueLogger(LogLevel.Warning);

        /// <summary>
        /// 错误日志队列
        /// </summary>
        private static readonly QueueLogger _errorLogger = new QueueLogger(LogLevel.Error);
        
        /// <summary>
        /// 日志接口
        /// </summary>
        private static readonly Lazy<ILogger> _logger= new Lazy<ILogger>(() =>
        {
            //创建日志
            LoggerFactory factory = new LoggerFactory();
            foreach (ILoggerProvider loggerProvider in LogConfig.Loggers)
            {
                factory.AddProvider(loggerProvider);
            }
            factory.AddProvider(_warningLogger);
            factory.AddProvider(_errorLogger);
            return factory.CreateLogger("log");
        });

        /// <summary>
        /// 日志接口
        /// </summary>
        public static ILogger Logger => _logger.Value;

        /// <summary>
        /// 警告日志队列
        /// </summary>
        public static ConcurrentQueue<string> Warnings => _warningLogger.Queue;

        /// <summary>
        /// 错误日志队列
        /// </summary>
        public static ConcurrentQueue<string> Errors => _errorLogger.Queue;
    }
}
