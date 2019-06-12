
namespace Kakegurui.Web.Models
{
    /// <summary>
    /// 交通数据单元
    /// </summary>
    public abstract class TrafficItem
    {
        /// <summary>
        /// 数据编号
        /// </summary>
        public abstract int ItemId { get; }

        /// <summary>
        /// 数据名称
        /// </summary>
        public abstract string ItemName { get; }

        /// <summary>
        /// 设备ip
        /// </summary>
        public abstract string Ip { get;}

        /// <summary>
        /// 设备端口
        /// </summary>
        public abstract int Port { get; }

        /// <summary>
        /// 通道编号
        /// </summary>
        public abstract int ChannelIndex { get; set; }

        /// <summary>
        /// 数据序号
        /// </summary>
        public abstract int ItemIndex { get; }

    }
}
