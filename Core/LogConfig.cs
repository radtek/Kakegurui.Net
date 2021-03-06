﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 当前程序配置文件读写类
    /// </summary>
    public static class LogConfig
    {
        /// <summary>
        /// 配置实例
        /// </summary>
        private static IConfigurationRoot _config;

        /// <summary>
        /// 设置日志配置文件的路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void InitConfig(string filePath)
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile(filePath)
                .Build();
        }

        /// <summary>
        /// 配置实例
        /// </summary>
        public static IConfigurationRoot Config => _config ?? (_config = new ConfigurationBuilder()
                                                       .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                                                       .AddJsonFile("appsettings.json")
                                                       .Build());
        /// <summary>
        /// 文件日志保存目录
        /// </summary>
        public static bool LogDebug => Config.GetValue("Log:Debug",false);

        /// <summary>
        /// 文件日志保存目录
        /// </summary>
        public static string LogDirectory => Config.GetValue("Log:Directory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../log/"));

        /// <summary>
        /// 文件日志保存天数
        /// </summary>
        public static int LogHoldDays => Config.GetValue("Log:HoldDays",0);

        /// <summary>
        /// 日志队列保存数量
        /// </summary>
        public static int LogQueueMaxCount => Config.GetValue("Log:QueueMaxCount", 10);

        /// <summary>
        /// 读取日志级别
        /// </summary>
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回日志级别，否则返回Critical(0)</returns>
        private static LogLevel GetLogLevel(string key)
        {
            string level = Config.GetValue<string>(key);
            return Enum.TryParse(level, out LogLevel l) ? l : LogLevel.None;
        }

        /// <summary>
        /// 获取日志提供者
        /// </summary>
        /// <param name="key">日志键</param>
        /// <returns>日志提供者集合</returns>
        private static List<ILoggerProvider> GetLogProvicers(string key)
        {
            List<ILoggerProvider> providers = new List<ILoggerProvider>();
            for (int i = 0; ; ++i)
            {
                string value = Config.GetValue<string>($"Log:{key}:{i}:Type");
                if (value == null)
                {
                    break;
                }
                else
                {
                    LogLevel minLevel = GetLogLevel($"Log:{key}:{i}:MinLevel");
                    if (minLevel == LogLevel.None)
                    {
                        minLevel = LogLevel.Trace;
                    }
                    LogLevel maxLevel = GetLogLevel($"Log:{key}:{i}:MaxLevel");
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
                            string name = Config.GetValue<string>($"Log:{key}:{i}:Name");
                            providers.Add(new FileLogger(minLevel, maxLevel, name));
                            break;
                        }
                    }
                }
            }
            return providers;
        }

        /// <summary>
        /// 从配置文件读取日志集合
        /// </summary>
        /// <returns></returns>
        public static List<ILoggerProvider> Loggers => GetLogProvicers("Logger");

        /// <summary>
        /// 从配置文件读取asp.net日志
        /// </summary>
        public static List<ILoggerProvider> WebLoggers => GetLogProvicers("WebLogger");
    }
}
