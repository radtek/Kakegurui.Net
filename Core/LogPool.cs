using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志
    /// </summary>
    public static class LogPool
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        static LogPool()
        {
            //创建文件目录和删除超时文件
            string directory = AppConfig.ReadString("log:file:directory") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../log/");
            int holdDays = AppConfig.ReadInt32("log:file:holddays") ?? 0;
            if (Directory.Exists(directory))
            {
                DeleteFiles(directory, holdDays);
            }
            else
            {
                Directory.CreateDirectory(directory);
            }

            //从配置文件添加日志
            LoggerFactory factoty = new LoggerFactory();
            for (int i = 0;; ++i)
            {
                ILoggerProvider provider = ReadProvider(i);
                if (provider == null)
                {
                    break;
                }
                else
                {
                    factoty.AddProvider(provider);
                }
            }

            Logger = factoty.CreateLogger("log");
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="directory">目录</param>
        /// <param name="holdDays">保存天数</param>
        private static void DeleteFiles(string directory,int holdDays)
        {
            foreach (string filePath in Directory.GetFiles(directory))
            {
                string[] datas = Path.GetFileNameWithoutExtension(filePath)
                    .Split("_", StringSplitOptions.RemoveEmptyEntries);
                if (datas.Length >= 2)
                {
                    DateTime fileDate = DateTime.ParseExact(datas[datas.Length - 1], "yyMMdd",
                        CultureInfo.CurrentCulture);
                    if ((DateTime.Today - fileDate).TotalDays >= holdDays)
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
            }
        }

        /// <summary>
        /// 日志接口
        /// </summary>
        public static ILogger Logger { get; }

        /// <summary>
        /// 从配置文件读取日志
        /// </summary>
        /// <param name="index">日志配置文件中的配置顺序</param>
        /// <returns>如果存在该配置返回实例，否则返回null</returns>
        private static ILoggerProvider ReadProvider(int index)
        {
            string value = AppConfig.ReadString(string.Format("log:logger:{0}:type", index));
            if (value != null)
            {
                ILogFilter filter = ReadFilter(index);
                if (filter != null)
                {
                    switch (value)
                    {
                        case "Console":
                        {
                            return new ConsoleLogger(filter);
                        }
                        case "File":
                        {
                            string name = AppConfig.ReadString(string.Format("log:logger:{0}:name", index));
                            return new FileLogger(filter,name);
                        }
                        default:
                            return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 读取日志的筛选方式
        /// </summary>
        /// <param name="index">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回筛选接口，否则返回null</returns>
        private static ILogFilter ReadFilter(int index)
        {
            string filter = AppConfig.ReadString(string.Format("log:logger:{0}:filter", index));
            switch (filter)
            {
                case null:
                    return null;
                case "Equal":
                {
                    LogLevel level = ReadLevel(string.Format("log:logger:{0}:level", index));
                    return new EqualFilter(level);
                }
                case "NotEqueal":
                {
                    LogLevel level = ReadLevel(string.Format("log:logger:{0}:level", index));
                    return new EqualFilter(level);
                }
                case "Higher":
                {
                    LogLevel level = ReadLevel(string.Format("log:logger:{0}:level", index));
                    return new HigherFilter(level);
                }
                case "HigherEqual":
                {
                    LogLevel level = ReadLevel(string.Format("log:logger:{0}:level", index));
                    return new HigherEqualFilter(level);
                }
                case "Range":
                {
                    LogLevel minLevel = ReadLevel(string.Format("log:logger:{0}:minlevel", index));
                    LogLevel maxLevel = ReadLevel(string.Format("log:logger:{0}:maxlevel", index));
                    return new RangeFilter(minLevel,maxLevel);
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// 读取日志级别
        /// </summary>
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回日志级别，否则返回Critical(0)</returns>
        private static LogLevel ReadLevel(string key)
        {
            string level= AppConfig.ReadString(key);
            return Enum.TryParse(level, out LogLevel l) ? l : LogLevel.None;
        }
    }
}
