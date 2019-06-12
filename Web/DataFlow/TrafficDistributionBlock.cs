using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Core;
using Kakegurui.Web.Models;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 交通数据分支数据块
    /// </summary>
    public class TrafficDistributionBlock<T> : TrafficActionBlock<T> where T : TrafficData
    {
        /// <summary>
        /// 数据键格式
        /// </summary>
        private const string KeyFormat="{0}:{1}_{2}_{3}";

        /// <summary>
        /// 车道或区域集合
        /// </summary>
        protected readonly Dictionary<string, int> _dataIds = new Dictionary<string, int>();
        
        /// <summary>
        /// 数据块集合
        /// </summary>
        protected readonly Dictionary<int, ITrafficDataBlock<T>> _blocks = new Dictionary<int, ITrafficDataBlock<T>>();

        /// <summary>
        /// 车道或区域计数器集合
        /// </summary>
        public Dictionary<string, int> DataCounts { get; }= new Dictionary<string, int>();

        /// <summary>
        /// 当前接收数据总数
        /// </summary>
        public int Total { get; private set; }

        /// <summary>
        /// 当前接收未识别数据总数
        /// </summary>
        public int Unknown { get; private set; }

        /// <summary>
        /// 新增数据块
        /// </summary>
        /// <param name="item">数据单元</param>
        /// <param name="targetBlock">数据块值</param>
        public void AddBlock(TrafficItem item, ITrafficDataBlock<T> targetBlock)
        {
            string key = string.Format(KeyFormat,item.Ip,item.Port,item.ChannelIndex,item.ItemIndex);
            _dataIds.Add(key, item.ItemId);
            DataCounts.Add(key, 0);
            _blocks.Add(item.ItemId, targetBlock);
        }

        /// <summary>
        /// 移除数据块
        /// </summary>
        /// <param name="deviceDataId">设备数据的唯一标识</param>
        /// <returns>如果存在键则返回数据块，否则返回null</returns>
        public ITrafficDataBlock<T> RemoveBlock(string deviceDataId)
        {
            ITrafficDataBlock<T> temp = null;
            if (_dataIds.ContainsKey(deviceDataId))
            {
                int dataId = _dataIds[deviceDataId];
                _dataIds.Remove(deviceDataId);
                DataCounts.Remove(deviceDataId);
                temp = _blocks[dataId];
                _blocks.Remove(dataId);
            }
            return temp;
        }

        /// <summary>
        /// 分配交通数据
        /// </summary>
        /// <param name="t">交通数据</param>
        protected override void Handle(T t)
        {
            string key = string.Format(KeyFormat, t.Ip, t.Port, t.ChannelIndex, t.ItemIndex);
            if (_dataIds.ContainsKey(key))
            {
                t.ItemId = _dataIds[key];
                DataCounts[key] += 1;
                _blocks[t.ItemId].InputBlock.Post(t);
                ++Total;
            }
            else
            {
                ++Unknown;
                LogPool.Logger.LogWarning("未配置的车道{0}:{1}-{2}-{3}", t.Ip, t.Port, t.ChannelIndex, t.ItemIndex);
            }
        }

        public override void WaitCompletion()
        {
            base.WaitCompletion();
            foreach (var laneBlock in _blocks)
            {
                laneBlock.Value.InputBlock.Complete();
                laneBlock.Value.WaitCompletion();
            }
        }
    }

}
