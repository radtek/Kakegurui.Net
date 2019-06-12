using System.Collections.Generic;

namespace Kakegurui.Monitor
{
    /// <summary>
    /// 监控状态
    /// </summary>
    public class MonitorStatus
    {
        /// <summary>
        /// cpu
        /// </summary>
        public string Cpu { get; set; }
        /// <summary>
        /// 内存(mb)
        /// </summary>
        public string Memory { get; set; }
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// 数据源连接状态集合
        /// </summary>
        public List<string> Connections { get; set; }

        /// <summary>
        /// 数据接收器状态集合
        /// </summary>
        public List<string> Adapters { get; set; }

        /// <summary>
        /// 数据入库状态集合
        /// </summary>
        public List<string> Branchs { get; set; }

        /// <summary>
        /// 定时任务集合
        /// </summary>
        public List<string> FixedJobs { get; set; }
        /// <summary>
        /// 警告日志集合
        /// </summary>
        public List<string> WarningLogs { get; set; }
        /// <summary>
        /// 错误日志集合
        /// </summary>
        public List<string> ErrorLogs { get; set; }
    }
}
