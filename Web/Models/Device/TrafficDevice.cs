using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kakegurui.Web.Codes;
using Kakegurui.Web.Models.Device;
using MySql.Data.EntityFrameworkCore.DataAnnotations;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 设备
    /// </summary>
    public class TrafficDevice
    {
        public TrafficDevice()
        {
            Status = DeviceStatus.异常;
            Marked = false;
        }

        protected TrafficDevice(DeviceType type)
        {
            Status = DeviceStatus.异常;
            Marked = false;
            Type = type;
        }

        /// <summary>
        /// 设备编码
        /// </summary>
        [Column("Id", TypeName = "INT")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [Column("Name", TypeName = "VARCHAR(200)")]
        [MySqlCharset("utf8")]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// 设备类型
        /// </summary>
        [NotMapped]
        public DeviceType Type { get; }

        /// <summary>
        /// 设备状态
        /// </summary>
        [Required]
        [EnumDataType(typeof(DeviceStatus))]
        [Column("Status", TypeName = "INT")]
        public DeviceStatus Status { get; set; }

        public string Status_Desc => Status.ToString();

        /// <summary>
        /// ip
        /// </summary>
        [Column("Ip", TypeName = "VARCHAR(15)")]
        [IPAddress]
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [Column("Port", TypeName = "INT")]
        [Required]
        [Range(1,65525)]
        public int Port { get; set; }

        /// <summary>
        /// 是否已标注
        /// </summary>
        [Required]
        [Column("Marked", TypeName = "BIT")]
        public bool Marked { get; set; }
        
        /// <summary>
        /// 点的位置
        /// </summary>
        [Column("Location", TypeName = "VARCHAR(100)")]
        public string Location { get; set; }
    }

    /// <summary>
    /// 设备标记更新
    /// </summary>
    public class TrafficDeviceLocation
    {
        /// <summary>
        /// 设备编码
        /// </summary>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 标记点的坐标
        /// </summary>
        [Required]
        public string Location { get; set; }
    }

}
