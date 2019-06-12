using System;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Web.DataFlow;
using Kakegurui.Web.Models;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 流量数据源数据块
    /// </summary>
    public abstract class TrafficActionBlock<T> : ITrafficDataBlock<T> where T : TrafficData
    {
        /// <summary>
        /// 入库数据块
        /// </summary>
        protected readonly ActionBlock<T> _actionBlock;

        /// <summary>
        /// 当前等待处理的数量
        /// </summary>
        public int InputCount => _actionBlock.InputCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">线程数</param>
        protected TrafficActionBlock(int threadCount = 1)
        {
            _actionBlock = new ActionBlock<T>(
                new Action<T>(Handle),new ExecutionDataflowBlockOptions{MaxDegreeOfParallelism = threadCount});
        }

        /// <summary>
        /// 处理交通数据
        /// </summary>
        /// <param name="t">交通数据</param>
        protected abstract void Handle(T t);


        #region 实现ITrafficDataBlock
        public ITargetBlock<T> InputBlock => _actionBlock;

        public virtual void WaitCompletion()
        {
            _actionBlock.Completion.Wait();
        }
        #endregion

    }

}
