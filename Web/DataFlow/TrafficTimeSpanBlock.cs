using System;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Core;
using Kakegurui.Web.Models;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 一个指定时间间隔的交通数据统计数据块
    /// </summary>
    public abstract class TrafficTimeSpanBlock<T> : TrafficActionBlock<T> where T : TrafficData
    {
        /// <summary>
        /// 当前接受数据的最小时间
        /// </summary>
        private DateTime _minTime;

        /// <summary>
        /// 当前接受数据的最大时间
        /// </summary>
        private DateTime _maxTime;

        /// <summary>
        /// 时间级别
        /// </summary>
        protected readonly DateTimeLevel _level;

        /// <summary>
        /// 统计成功后发送的数据块
        /// </summary>
        protected ITargetBlock<T> _targetBlock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="level">时间级别</param>
        protected TrafficTimeSpanBlock(DateTimeLevel level)
        {
            _level = level;
            _minTime = DateTime.MinValue;
            _maxTime = DateTime.MinValue;
        }

        /// <summary>
        /// 连接数据块
        /// </summary>
        /// <param name="targetBlock">数据块</param>
        public void LinkBlock(ITargetBlock<T> targetBlock)
        {
            _targetBlock = targetBlock;
        }

        /// <summary>
        /// 处理数据块中的流量数据
        /// </summary>
        /// <param name="data">流量数据</param>
        protected override void Handle(T data)
        {
            if(data.DateTime >= _maxTime)
            {
                //LogPool.Logger.LogDebug($"触发超过时段 {Name} {data.DateTime} {_minTime} {_maxTime}");
                OutRange(data);
                _minTime = TimePointConvert.CurrentTimePoint(_level, data.DateTime);
                _maxTime = TimePointConvert.NextTimePoint(_level, _minTime);
                InRange(data, _minTime);
            }
            else if (data.DateTime >= _minTime && data.DateTime < _maxTime)
            {
                //LogPool.Logger.LogDebug($"时间范围内 {Name} {_minTime} {_maxTime}");
                InRange(data,_minTime);
            }
            else
            {
                //LogPool.Logger.LogDebug($"时间范围外 {Name} {_minTime} {_maxTime}");
            }
        }

        /// <summary>
        /// 在时间范围内
        /// </summary>
        /// <param name="t">数据</param>
        /// <param name="dateTime">数据时间</param>
        protected abstract void InRange(T t,DateTime dateTime);

        /// <summary>
        /// 超过当前时间范围
        /// </summary>
        /// <param name="t">数据</param>
        protected abstract void OutRange(T t);
    }
}
