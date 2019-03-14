using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 自动锁
    /// </summary>
    public static class AutoLock
    {
        /// <summary>
        /// 同步锁
        /// </summary>
        /// <param name="obj">锁定实例</param>
        /// <param name="action">执行方法</param>
        public static void Lock(object obj,Action action)
        {
            StackTrace stack = new StackTrace(true);

            StackFrame[] frame = stack.GetFrames();

            if (Monitor.TryEnter(obj, AppConfig.LockTimeout))
            {
                try
                {
                    action();
                }
                finally
                {
                    Monitor.Exit(obj);
                }
            }
            else
            {
                LogPool.Logger.LogWarning("lock {0} ", frame[0]);
            }
        }

        /// <summary>
        /// 同步锁
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="obj">锁定实例</param>
        /// <param name="func">执行方法</param>
        /// <returns>返回实例</returns>
        public static T Lock<T>(object obj, Func<T> func) where T :class
        {
            StackTrace stack = new StackTrace(true);

            StackFrame[] frame = stack.GetFrames();

            if (Monitor.TryEnter(obj, AppConfig.LockTimeout))
            {
                try
                {
                    return func();
                }
                finally
                {
                    Monitor.Exit(obj);
                }
            }
            else
            {
                LogPool.Logger.LogWarning("lock {0} ", frame[0]);
            }

            return null;
        }

    }
}
