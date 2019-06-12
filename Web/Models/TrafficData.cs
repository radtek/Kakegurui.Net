using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kakegurui.Web.Models
{
    public abstract class TrafficData
    {
        /// <summary>
        /// 数据编号
        /// </summary>
        public abstract int ItemId { get; set; }

        /// <summary>
        /// 数据时间
        /// </summary>
        public abstract DateTime DateTime { get; set; }

        /// <summary>
        /// 设备ip
        /// </summary>
        [NotMapped]
        public string Ip { get; set; }

        /// <summary>
        /// 设备端口
        /// </summary>
        [NotMapped]
        public int Port { get; set; }

        /// <summary>
        /// 通道序号
        /// </summary>
        [NotMapped]
        public int ChannelIndex { get; set; }

        /// <summary>
        /// 通道下车道或区域序号
        /// </summary>
        [NotMapped]
        public int ItemIndex { get; set; }
    }

}
