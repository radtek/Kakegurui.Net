using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 当前程序配置文件读写类
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// 日志接口
        /// </summary>
        private static readonly Lazy<IConfigurationRoot> _config = new Lazy<IConfigurationRoot>(() => new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build());

        /// <summary>
        /// 配置接口
        /// </summary>
        public static IConfigurationRoot Config => _config.Value;

        /// <summary>
        /// 重连间隔时间(秒)
        /// </summary>
        public static int ConnectionSpan => Config.GetValue("ConnectionSpan", 5);

        /// <summary>
        /// 监控日志输出间隔时间(秒)
        /// </summary>
        public static int MonitorSpan => Config.GetValue("MonitorSpan", 60);

        /// <summary>
        /// 文件日志保存目录
        /// </summary>
        public static string LogDirectory => Config.GetValue("Log:Directory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../log/"));

        /// <summary>
        /// 文件日志保存天数
        /// </summary>
        public static int LogHoldDays => Config.GetValue("Log:HoldDays",0);

        /// <summary>
        /// 从配置文件读取日志集合
        /// </summary>
        /// <returns></returns>
        public static List<ILoggerProvider> Loggers
        {
            get
            {
                List<ILoggerProvider> providers = new List<ILoggerProvider>();
                for (int i = 0; ; ++i)
                {
                    string value = Config.GetValue<string>($"Log:Logger:{i}:Type");
                    if (value == null)
                    {
                        break;
                    }
                    else
                    {
                        LogLevel minLevel = GetLogLevel($"Log:Logger:{i}:MinLevel");
                        if (minLevel == LogLevel.None)
                        {
                            minLevel = LogLevel.Trace;
                        }
                        LogLevel maxLevel = GetLogLevel($"Log:Logger:{i}:MaxLevel");
                        if (maxLevel == LogLevel.None)
                        {
                            maxLevel = LogLevel.Critical;
                        }
                        switch (value)
                        {
                            case "Console":
                            {
                                providers.Add(new ConsoleLogger(minLevel, maxLevel));
                                break;
                            }
                            case "File":
                            {
                                string name = Config.GetValue<string>($"Log:Logger:{i}:Name");
                                providers.Add(new FileLogger(minLevel, maxLevel, name));
                                break;
                            }
                        }
                    }
                }
                return providers;
            }
        }

        /// <summary>
        /// 读取日志级别
        /// </summary>
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回日志级别，否则返回Critical(0)</returns>
        public static LogLevel GetLogLevel(string key)
        {
            string level = Config.GetValue<string>(key);
            return Enum.TryParse(level, out LogLevel l) ? l : LogLevel.None;
        }
    }
}
