using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 区域
    /// </summary>
    public class TrafficDensityRegion:TrafficItem
    {
        /// <summary>
        /// 区域编码
        /// </summary>
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("RegionId", TypeName = "INT")]
        public int RegionId { get; set; }

        /// <summary>
        /// 区域序号
        /// </summary>
        [Required]
        [Column("RegionIndex", TypeName = "INT")]
        public int RegionIndex { get; set; }

        /// <summary>
        /// 区域名称
        /// </summary>
        [Required]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        [Column("RegionName", TypeName = "VARCHAR(200)")]
        [MySqlCharset("utf8")]
        public string RegionName { get; set; }

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
        /// 绘制区域
        /// </summary>
        [Required]
        [Column("Region", TypeName = "Text")]
        public string Region { get; set; }

        /// <summary>
        /// 是否是重点区域
        /// </summary>
        [Required]
        [Column("IsVip", TypeName = "Bit")]
        public bool IsVip { get; set; }

        /// <summary>
        /// 饱和值
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Saturation", TypeName = "INT")]
        public int Saturation { get; set; }

        /// <summary>
        /// 警戒值
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Warning", TypeName = "INT")]
        public int Warning { get; set; }

        /// <summary>
        /// 报警时间限制
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("WarningDuration", TypeName = "INT")]
        public int WarningDuration { get; set; }

        /// <summary>
        /// 密集度
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Density", TypeName = "INT")]
        public int Density { get; set; }

        /// <summary>
        /// 密集度范围
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("DensityRange", TypeName = "INT")]
        public int DensityRange { get; set; }

        /// <summary>
        /// 车辆数
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("CarCount", TypeName = "INT")]
        public int CarCount { get; set; }

        /// <summary>
        /// 连续次数
        /// </summary>
        [Required]
        [Range(1, 10000)]
        [Column("Frequency", TypeName = "INT")]
        public int Frequency { get; set; }

        [NotMapped]
        public override string Ip => Channel?.DensityDevice?.Ip;

        [NotMapped]
        public override int Port => Channel?.DensityDevice?.Port ?? 0;

        [NotMapped]
        public override int ItemId => RegionId;

        [NotMapped]
        public override string ItemName => RegionName;

        [NotMapped]
        public override int ItemIndex => RegionIndex;

        [JsonIgnore]
        public TrafficDensityChannel Channel { get; set; }
    }
}
