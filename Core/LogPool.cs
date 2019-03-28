using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Configuration;
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
            string directory = AppConfig.LogDirectory;
            int holdDays = AppConfig.LogHoldDays;
            if (Directory.Exists(directory))
            {
                DeleteFiles(directory, holdDays);
            }
            else
            {
                Directory.CreateDirectory(directory);
            }
        }

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

    }
}
