using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 控制台日志类
    /// </summary>
    public class ConsoleLogger : Logger
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="filter">日志筛选接口</param>
        public ConsoleLogger(ILogFilter filter)
            :base(filter)
        {

        }

        protected override void LogCore(string log)
        {
            Console.WriteLine(log);
        }

    };
}
