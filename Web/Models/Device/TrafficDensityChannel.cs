using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 高点通道
    /// </summary>
    public class TrafficDensityChannel : TrafficChannel
    {
        /// <summary>
        /// 区域集合
        /// </summary>
        public List<TrafficDensityRegion> Regions { get; set; }

        [JsonIgnore]
        public TrafficDensityDevice DensityDevice { get; set; }

    }
}
