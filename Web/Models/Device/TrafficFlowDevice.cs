using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.Web.Codes;
using Kakegurui.Web.Codes.Flow;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 流量设备
    /// </summary>
    public class TrafficFlowDevice : TrafficDevice
    {
        public TrafficFlowDevice()
            : base(DeviceType.Flow)
        {

        }

        /// <summary>
        /// 设备类型
        /// </summary>
        [Required]
        [Column("FlowDeviceType", TypeName = "INT")]
        [EnumDataType(typeof(FlowDeviceType))]
        public FlowDeviceType FlowDeviceType { get; set; }

        /// <summary>
        /// 设备类型描述
        /// </summary>
        [NotMapped]
        public string FlowDeviceType_Desc => FlowDeviceType.ToString();

        /// <summary>
        /// 授权状态
        /// </summary>
        [Column("License", TypeName = "VARCHAR(100)")]
        public string License { get; set; }

        /// <summary>
        /// 硬盘空间
        /// </summary>
        [Column("Space", TypeName = "VARCHAR(100)")]
        public string Space { get; set; }

        /// <summary>
        /// 系统时间
        /// </summary>
        [Column("Systime", TypeName = "VARCHAR(100)")]
        public string Systime { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        [Column("Runtime", TypeName = "VARCHAR(100)")]
        public string Runtime { get; set; }

        /// <summary>
        /// 通道集合
        /// </summary>
        public List<TrafficFlowChannel> Channels { get; set; }
    }
}
