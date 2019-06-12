using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Core;
using Kakegurui.Web.DataFlow;
using Kakegurui.Web.Models;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 交通数据源数据块
    /// </summary>
    public abstract class TrafficBranchBlock<T> where T : TrafficData
    {

        /// <summary>
        /// 当前接受数据的最小时间点
        /// </summary>
        private DateTime _minTime;

        /// <summary>
        /// 当前接受数据的最大时间点
        /// </summary>
        private DateTime _maxTime;

        /// <summary>
        /// 当前时间分支的数据块
        /// </summary>
        private BufferBlock<T> _currentBlock = new BufferBlock<T>();

        /// <summary>
        /// 下一个时间分支的数据块
        /// </summary>
        private BufferBlock<T> _nextBlock = new BufferBlock<T>();

        /// <summary>
        /// 源头数据块
        /// </summary>
        protected TrafficDistributionBlock<T> _distributionBlock;

        /// <summary>
        /// 当前时间分支数据块的释放接口
        /// </summary>
        private IDisposable _currentDisposable;

        /// <summary>
        /// 服务实例提供者
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 批处理数量
        /// </summary>
        protected readonly int _batchSize;

        /// <summary>
        /// 入库线程数
        /// </summary>
        protected readonly int _threadCount;

        /// <summary>
        /// 设备集合
        /// </summary>
        protected List<TrafficItem> _items = new List<TrafficItem>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceProvider">服务实例提供者</param>
        /// <param name="items">数据单元集合</param>
        /// <param name="minTime">最小时间</param>
        /// <param name="maxTime">最大时间</param>
        /// <param name="batchSize">批处理数量</param>
        /// <param name="threadCount">入库线程数</param>
        protected TrafficBranchBlock(IServiceProvider serviceProvider, List<TrafficItem> items,DateTime minTime,DateTime maxTime,int batchSize,int threadCount)
        {
            _serviceProvider = serviceProvider;
            _items.AddRange(items);

            _minTime = minTime;
            _maxTime = maxTime;

            _batchSize = batchSize;
            _threadCount = threadCount;
        }

        /// <summary>
        /// 创建数据块
        /// </summary>
        /// <returns></returns>
        protected abstract ITargetBlock<T> CreateDataBlock();

        /// <summary>
        /// 保存当前数据块
        /// </summary>
        protected abstract void SaveCurrentCore();

        /// <summary>
        /// 获取交通数据单元的键
        /// </summary>
        /// <param name="item">交通数据单元</param>
        /// <returns>键</returns>
        protected string GetItemKey(TrafficItem item)
        {
            return $"{item.Ip}:{item.Port}_{item.ChannelIndex}_{item.ItemIndex}";
        }

        /// <summary>
        /// 添加交通单元数据
        /// </summary>
        /// <param name="item">交通单元数据</param>
        protected abstract void AddItemBlock(TrafficItem item);

        /// <summary>
        /// 移除交通单元数据
        /// </summary>
        /// <param name="item">交通单元数据</param>
        protected virtual void RemoveItemBlock(TrafficItem item)
        {
            ITrafficDataBlock<T> block = _distributionBlock.RemoveBlock(GetItemKey(item));
            if (block != null)
            {
                block.InputBlock.Complete();
                block.WaitCompletion();
            }
        }

        /// <summary>
        /// 打开当前数据源
        /// </summary>
        public void Open()
        {
            _currentDisposable = _currentBlock.LinkTo(CreateDataBlock(), new DataflowLinkOptions { PropagateCompletion = true });
        }

        /// <summary>
        /// 重启
        /// </summary>
        /// <param name="items">数据单元集合</param>
        public void Reset(List<TrafficItem> items)
        {
            foreach (TrafficItem item in items)
            {
                bool existed = false;
                foreach (TrafficItem currentItem in _items)
                {
                    if (item.ItemId == currentItem.ItemId)
                    {
                        existed = true;
                        break;
                    }
                }

                if (!existed)
                {
                    AddItemBlock(item);
                }
            }

            foreach (TrafficItem currentItem in _items)
            {
                bool existed = false;
                foreach (TrafficItem item in items)
                {
                    if (currentItem.ItemId == item.ItemId)
                    {
                        existed = true;
                        break;
                    }
                }
                if (!existed)
                {
                    RemoveItemBlock(currentItem);
                }
            }
            _items = items;
        }

        /// <summary>
        /// 保存当前数据块的数据
        /// </summary>
        public void SaveCurrent()
        {
            _currentBlock.Complete();
            SaveCurrentCore();
        }

        /// <summary>
        /// 切换分支
        /// </summary>
        public void SwitchBranch(DateTime minTime)
        {
            _currentDisposable.Dispose();
            _currentDisposable = _nextBlock.LinkTo(CreateDataBlock(), new DataflowLinkOptions { PropagateCompletion = true });
            _currentBlock = _nextBlock;
            _minTime = _maxTime;
            _maxTime = minTime;
            _nextBlock = new BufferBlock<T>();
        }

        /// <summary>
        /// 向数据源发送流量数据
        /// </summary>
        /// <param name="flow">流量数据</param>
        public void Post(T flow)
        {
            if (flow.DateTime >= _minTime && flow.DateTime < _maxTime)
            {
                _currentBlock.Post(flow);
            }
            else if (flow.DateTime >= _maxTime)
            {
                _nextBlock.Post(flow);
            }
            else
            {
                LogPool.Logger.LogWarning("过期数据 {0}", flow.DateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        }

        public abstract string GetStatus();
    }
}
