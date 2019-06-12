
namespace Kakegurui.Web.Models
{
    /// <summary>
    /// 流量图表
    /// </summary>
    /// <typeparam name="T">横轴类型</typeparam>
    /// <typeparam name="U">纵轴类型</typeparam>
    public class TrafficChart<T, U>
    {
        public T Axis { get; set; }
        public U Value { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>
    /// 流量图表
    /// </summary>
    /// <typeparam name="T">横轴类型</typeparam>
    /// <typeparam name="U">纵轴类型</typeparam>
    /// <typeparam name="V">数据类型</typeparam>
    public class TrafficChart<T, U, V>
    {
        public T Axis { get; set; }
        public U Value { get; set; }
        public string Remark { get; set; }
        public V Data { get; set; }
    }

}
