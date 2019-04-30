using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Core
{
    /// <summary>
    /// 线程基类
    /// </summary>
    public abstract class TaskObject
    {
        /// <summary>
        /// 线程类
        /// </summary>
        private readonly Task _task;

        /// <summary>
        /// 线程取消功能
        /// </summary>
        private readonly CancellationTokenSource _source=new CancellationTokenSource();

        /// <summary>
        /// 供任务内部使用的取消判断
        /// </summary>
        protected CancellationToken _token;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">线程名</param>
        protected TaskObject(string name)
        {
            Name = name;
            _token = _source.Token;
            _task = new Task(Action, _token);
            HitPoint=DateTime.Now;
        }

        /// <summary>
        /// 线程名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 线程最后一次记录时间
        /// </summary>
        public DateTime HitPoint { get; private set; }

        /// <summary>
        /// 线程函数
        /// </summary>
        private void Action()
        {
            LogPool.Logger.LogInformation("{0} start",Name);
            try
            {
                ActionCore();
                LogPool.Logger.LogInformation("{0} stop", Name);
            }
            catch (Exception e)
            {
                LogPool.Logger.LogInformation(e,"{0} exit", Name);
            }
        }

        /// <summary>
        /// 供子类实现的线程执行函数
        /// </summary>
        protected abstract void ActionCore();

        /// <summary>
        /// 是否请求中止线程
        /// </summary>
        /// <returns>返回true表示请求中止线程，否则返回false</returns>
        protected bool IsCancelled()
        {
            HitPoint =DateTime.Now;
            return _token.IsCancellationRequested;
        }

        /// <summary>
        /// 开始线程
        /// </summary>
        public void Start()
        {
            _task.Start();
        }

        /// <summary>
        /// 停止线程
        /// </summary>
        public virtual void Stop()
        {
            _source.Cancel();
            try
            {
                _task.Wait(_token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
