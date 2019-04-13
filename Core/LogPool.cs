using System;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志
    /// </summary>
    public static class LogPool
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        private static readonly Lazy<ILogger> _logger= new Lazy<ILogger>(() =>
        {
            //创建日志
            LoggerFactory factory = new LoggerFactory();
            foreach (ILoggerProvider loggerProvider in AppConfig.Loggers)
            {
                factory.AddProvider(loggerProvider);
            }
            return factory.CreateLogger("log");
        });

        /// <summary>
        /// 日志接口
        /// </summary>
        public static ILogger Logger => _logger.Value;

    }
}
