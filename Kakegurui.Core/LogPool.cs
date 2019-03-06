using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日志
    /// </summary>
    public class LogPool
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        static LogPool()
        {
            //从配置文件添加日志
            int index = 1;
            ILoggerProvider provider;
            LoggerFactory factoty=new LoggerFactory();
            while ((provider = ReadProvider(string.Format("log.{0}", index))) != null)
            {
                factoty.AddProvider(provider);
                ++index;
            }

            Logger = factoty.CreateLogger("log");
        }

        /// <summary>
        /// 日志接口
        /// </summary>
        public static ILogger Logger { get; }

        /// <summary>
        /// 从配置文件读取日志
        /// </summary>
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>如果存在该配置返回实例，否则返回null</returns>
        private static ILoggerProvider ReadProvider(string key)
        {
            string value = AppConfig.ReadString(string.Format("{0}.type", key));
            if (value != null)
            {
                ILogFilter filter = ReadFilter(key);
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
                            string name = AppConfig.ReadString(string.Format("{0}.name", key));
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
        /// <param name="key">日志配置文件中的配置顺序</param>
        /// <returns>读取成功返回筛选接口，否则返回null</returns>
        private static ILogFilter ReadFilter(string key)
        {
            string filter = AppConfig.ReadString(string.Format("{0}.filter", key));
            switch (filter)
            {
                case null:
                    return null;
                case "Equal":
                {
                    LogLevel level = ReadLevel(string.Format("{0}.filter.level", key));
                    return new EqualFilter(level);
                }
                case "NotEqueal":
                {
                    LogLevel level = ReadLevel(string.Format("{0}.filter.level", key));
                    return new EqualFilter(level);
                }
                case "Higher":
                {
                    LogLevel level = ReadLevel(string.Format("{0}.filter.level", key));
                    return new HigherFilter(level);
                }
                case "HigherEqual":
                {
                    LogLevel level = ReadLevel(string.Format("{0}.filter.level", key));
                    return new HigherEqualFilter(level);
                }
                case "Range":
                {
                    LogLevel minLevel = ReadLevel(string.Format("{0}.filter.minlevel", key));
                    LogLevel maxLevel = ReadLevel(string.Format("{0}.filter.maxlevel", key));
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

            if (level == null)
            {
                return LogLevel.Critical;
            }

            switch (level)
            {
                case "Debug":
                    return LogLevel.Debug;
                case "Info":
                    return LogLevel.Information;
                case "Warning":
                    return LogLevel.Warning;
                case "Error":
                    return LogLevel.Error;
                default:
                    return LogLevel.Critical;
            }
        }
    }
}
