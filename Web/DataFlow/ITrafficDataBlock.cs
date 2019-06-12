using System.Threading.Tasks.Dataflow;
using Kakegurui.Web.Models;

namespace Kakegurui.Web.DataFlow
{
    /// <summary>
    /// 交通数据块接口
    /// </summary>
    /// <typeparam name="T">交通数据</typeparam>
    public interface ITrafficDataBlock<in T> where T : TrafficData
    {
        /// <summary>
        /// 数据入口
        /// </summary>
        ITargetBlock<T> InputBlock { get; }

        /// <summary>
        /// 结束数据块当前的数据处理
        /// </summary>
        void WaitCompletion();
    }
}