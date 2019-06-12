using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Web.Models;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 交通数据广播数据块
    /// </summary>
    public class TrafficBroadcastBlock<T> : ITrafficDataBlock<T> where T : TrafficData
    {
        /// <summary>
        /// 广播数据块
        /// </summary>
        private readonly BroadcastBlock<T> _broadcastBlock = new BroadcastBlock<T>(f => f);

        /// <summary>
        /// 流量数据块集合
        /// </summary>
        private readonly List<ITrafficDataBlock<T>> _subBlocks = new List<ITrafficDataBlock<T>>();

        /// <summary>
        /// 连接数据块
        /// </summary>
        /// <param name="targetBlock">数据块</param>
        public void LinkBlock(ITrafficDataBlock<T> targetBlock)
        {
            _broadcastBlock.LinkTo(targetBlock.InputBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _subBlocks.Add(targetBlock);
        }

        #region 实现ITrafficDataBlock
        public ITargetBlock<T> InputBlock => _broadcastBlock;

        public void WaitCompletion()
        {
            foreach (var subBlock in _subBlocks)
            {
                subBlock.WaitCompletion();
            }
        }
        #endregion

    }
}
