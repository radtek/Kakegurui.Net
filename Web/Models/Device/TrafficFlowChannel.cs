using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kakegurui.Web.Models.Device
{
    /// <summary>
    /// 流量通道
    /// </summary>
    public class TrafficFlowChannel : TrafficChannel
    {
        /// <summary>
        /// 车道集合
        /// </summary>
        public List<TrafficFlowLane> Lanes { get; set; }

        [JsonIgnore]
        public TrafficFlowDevice FlowDevice { get; set; }

    }
}
