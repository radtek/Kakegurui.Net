using System;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Web.Models;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 交通数据数组数据块
    /// </summary>
    /// <typeparam name="T">交通数据</typeparam>
    public abstract class TrafficArrayActionBlock<T> where T:TrafficData
    {
        /// <summary>
        /// 数据库数据块
        /// </summary>
        private readonly ActionBlock<T[]> _actionBlock;

        /// <summary>
        /// 当前等待处理的数量
        /// </summary>
        public int InputCount => _actionBlock.InputCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="threadCount">线程数</param>
        protected TrafficArrayActionBlock(int threadCount=1)
        {
            _actionBlock = new ActionBlock<T[]>(new Action<T[]>(Handle), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threadCount });
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="datas">数据集合</param>
        protected abstract void Handle(T[] datas);

        #region 实现ITrafficDbBlock
        public ITargetBlock<T[]> InputBlock => _actionBlock;

        public void WaitCompletion()
        {
            _actionBlock.Completion.Wait();
        }
        #endregion
    }
}
