using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MySql.Data.EntityFrameworkCore.DataAnnotations;
using Newtonsoft.Json;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 路口
    /// </summary>
    public class TrafficRoad
    {
        /// <summary>
        /// 路口编号
        /// </summary>
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 路口名称
        /// </summary>
        [Column("Name", TypeName = "VARCHAR(200)")]
        [MySqlCharset("utf8")]
        [Required]
        [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 1)]
        public string Name { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public List<TrafficFlowChannel> FlowChannels { get; set; }

        /// <summary>
        /// 通道
        /// </summary>
        [JsonIgnore]
        public List<TrafficDensityChannel> DensityChannels { get; set; }
    }
}
