using System;
using System.Diagnostics;

namespace Kakegurui.Monitor
{
    /// <summary>
    /// 系统监控
    /// </summary>
    public class SystemMonitor:IFixedJob
    {
        /// <summary>
        /// 上一次进程cpu的时间总和
        /// </summary>
        private TimeSpan _lastCpuTime;

        /// <summary>
        /// 上一次计算的时间
        /// </summary>
        private DateTime _lastTime;

        /// <summary>
        /// cpu占用率
        /// </summary>
        public string Cpu { get; private set; }

        /// <summary>
        /// 内存(mb)
        /// </summary>
        public string Memory { get; private set; }
        
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadCount { get; private set; }

        public void Handle(DateTime currentTime, DateTime nextTime)
        {
            DateTime now = DateTime.Now;
            Process process = Process.GetCurrentProcess();
            TimeSpan currentCpuTime = process.TotalProcessorTime;
            Cpu = $"{(currentCpuTime - _lastCpuTime).TotalMilliseconds / (now - _lastTime).TotalMilliseconds / Environment.ProcessorCount * 100:N2}";
            Memory = $"{process.WorkingSet64 / 1024.0 / 1024.0:N2}";
            ThreadCount = process.Threads.Count;
            _lastCpuTime = currentCpuTime;
            _lastTime = now;
        }
    }
}
