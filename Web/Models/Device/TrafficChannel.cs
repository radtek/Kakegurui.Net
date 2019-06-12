using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.Web.Codes;
using MySql.Data.EntityFrameworkCore.DataAnnotations;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 通道
    /// </summary>
    public class TrafficChannel
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Column("DeviceId", TypeName = "INT")]
        [Required]
        public int DeviceId { get; set; }

        /// <summary>
        /// 通道序号
        /// </summary>
        [Column("ChannelIndex", TypeName = "INT")]
        [Required]
        public int ChannelIndex { get; set; }

        /// <summary>
        /// 通道名称
        /// </summary>
        [Column("ChannelName", TypeName = "VARCHAR(200)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string ChannelName { get; set; }

        /// <summary>
        /// 通道类型
        /// </summary>
        [Column("ChannelType", TypeName = "INT")]
        [Required]
        [EnumDataType(typeof(ChannelType))]
        public ChannelType ChannelType { get; set; }

        public string ChannelType_Desc => ChannelType.ToString();

        /// <summary>
        /// 通道状态
        /// </summary>
        [Column("ChannelStatus", TypeName = "INT")]
        public DeviceStatus ChannelStatus { get; set; }

        public string ChannelStatus_Desc => ChannelStatus.ToString();

        /// <summary>
        /// rtsp地址
        /// </summary>
        [Column("Rtsp", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string RtspAddress { get; set; }

        /// <summary>
        /// rtsp用户名
        /// </summary>
        [Column("RtspUser", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string RtspUser { get; set; }

        /// <summary>
        /// rtsp密码
        /// </summary>
        [Column("RtspPassword", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string RtspPassword { get; set; }

        /// <summary>
        /// rtsp协议类型
        /// </summary>
        [Column("RtspProtocol", TypeName = "INT")]
        [EnumDataType(typeof(RtspProtocol))]
        public RtspProtocol? RtspProtocol { get; set; }

        public string RtspProtocol_Desc => RtspProtocol?.ToString();

        /// <summary>
        /// gb28181设备编号
        /// </summary>
        [Column("GBDeviceId", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string GbDeviceId { get; set; }

        /// <summary>
        /// 文件地址
        /// </summary>
        [Column("FilePath", TypeName = "VARCHAR(100)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string FilePath { get; set; }

        /// <summary>
        /// 关联路口
        /// </summary>
        [Column("RoadId", TypeName = "INT")]
        [Required]
        public int RoadId { get; set; }

        /// <summary>
        /// 关联路口
        /// </summary>
        public TrafficRoad Road { get; set; }
    }
}
