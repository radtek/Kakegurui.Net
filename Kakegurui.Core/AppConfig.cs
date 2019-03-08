using System;
using Microsoft.Extensions.Configuration;

namespace Kakegurui.Core
{
    /// <summary>
    /// 当前程序配置文件读写类
    /// </summary>
    public static class AppConfig
    {
        /// <summary>
        /// 锁超时时间
        /// </summary>
        public static int LockTimeout { get; }

        static AppConfig()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            LockTimeout = ReadInt32("LockTimeout") ?? 3000;
        }

        /// <summary>
        /// 读取appSettings配置文件内容
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>如果key不存在返回null，否则返回value</returns>
        public static string ReadString(string key)
        {
            IConfigurationSection section= Config.GetSection(key);
            return section?.Value;
        }

        /// <summary>
        /// 读取appSettings配置文件内容
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>布尔值</returns>
        public static bool ReadBoolean(string key)
        {
            string value = ReadString(key);
            return bool.TryParse(value, out var result) && result;
        }

        /// <summary>
        /// 读取appSettings配置文件内容
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>可空的数字</returns>
        public static int? ReadInt32(string key)
        {
            string value = ReadString(key);
            if (!int.TryParse(value, out var result))
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// 配置接口
        /// </summary>
        private static readonly IConfigurationRoot Config;
    }
}
