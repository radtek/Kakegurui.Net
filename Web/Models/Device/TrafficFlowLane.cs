using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.Web.Codes.Flow;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 车道
    /// </summary>
    public class TrafficFlowLane:TrafficItem
    {
        /// <summary>
        /// 车道编号
        /// </summary>
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("LaneId", TypeName = "INT")]
        public int LaneId { get; set; }

        /// <summary>
        /// 车道序号
        /// </summary>
        [Required]
        [Column("LaneIndex", TypeName = "INT")]
        public int LaneIndex { get; set; }

        /// <summary>
        /// 车道名称
        /// </summary>
        [Column("LaneName", TypeName = "VARCHAR(200)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string LaneName{ get; set; }

        /// <summary>
        /// 通道序号
        /// </summary>
        [Required]
        [Column("DeviceId", TypeName = "INT")]
        public int DeviceId { get; set; }

        /// <summary>
        /// 通道序号
        /// </summary>
        [Required]
        [Column("ChannelIndex", TypeName = "INT")]
        public override int ChannelIndex { get; set; }

        /// <summary>
        /// 车道方向
        /// </summary>
        [Column("Direction", TypeName = "INT")]
        [Required]
        [EnumDataType(typeof(Direction))]
        public Direction Direction { get; set; }

        /// <summary>
        /// 车道方向描述
        /// </summary>
        [NotMapped]
        public string Direction_Desc => Direction.ToString();

        /// <summary>
        /// 车道流向
        /// </summary>
        [Column("FlowDirection", TypeName = "INT")]
        [Required]
        [EnumDataType(typeof(FlowDirection))]
        public FlowDirection FlowDirection { get; set; }

        /// <summary>
        /// 车道流向描述
        /// </summary>
        [NotMapped]
        public string FlowDirection_Desc => FlowDirection.ToString();

        /// <summary>
        /// 绘制区域
        /// </summary>
        [Column("Region", TypeName = "Text")]
        [Required]
        public string Region { get; set; }

        /// <summary>
        /// IO地址
        /// </summary>
        [Column("IOIp", TypeName = "VARCHAR(15)")]
        [IPAddress]
        public string IOIp { get; set; }

        /// <summary>
        /// IO端口
        /// </summary>
        [Column("IOPort", TypeName = "INT")]
        [Range(1, 65525)]
        public int? IOPort { get; set; }

        /// <summary>
        /// IO序号
        /// </summary>
        [Column("IOIndex", TypeName = "INT")]
        public int? IOIndex { get; set; }

        /// <summary>
        /// 车道性质
        /// </summary>
        [Column("LaneType", TypeName = "INT")]
        [EnumDataType(typeof(LaneType))]
        public LaneType? LaneType { get; set; }

        /// <summary>
        /// 车道性质描述
        /// </summary>
        public string LaneType_Desc => LaneType?.ToString();

        [NotMapped]
        public override string Ip => Channel?.FlowDevice?.Ip;

        [NotMapped]
        public override int Port => Channel?.FlowDevice?.Port ?? 0;

        [NotMapped]
        public override int ItemId => LaneId;

        [NotMapped]
        public override string ItemName => LaneName;

        [NotMapped]
        public override int ItemIndex => LaneIndex;

        [JsonIgnore]
        public TrafficFlowChannel Channel { get; set; }

    }
}
