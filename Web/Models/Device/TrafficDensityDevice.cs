using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.Web.Codes;
using Kakegurui.Web.Codes.Density;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 流量设备
    /// </summary>
    public class TrafficDensityDevice : TrafficDevice
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TrafficDensityDevice()
            : base(DeviceType.Density)
        {

        }

        /// <summary>
        /// 高点设备型号
        /// </summary>
        [Required]
        [Column("DensityDeviceType", TypeName = "INT")]
        [EnumDataType(typeof(DensityDeviceType))]
        public DensityDeviceType DensityDeviceType { get; set; }

        /// <summary>
        /// 高点设备型号描述
        /// </summary>
        [NotMapped]
        public string DensityDeviceType_Desc => DensityDeviceType.ToString();

        /// <summary>
        /// 高点通道集合
        /// </summary>
        public List<TrafficDensityChannel> Channels { get; set; }
    }
}
