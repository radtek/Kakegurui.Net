using System;
using Microsoft.AspNetCore.Mvc;

namespace Kakegurui.Web.Controllers
{
    [Produces("application/json")]
    [Route("api/Monitor")]
    public class MonitorController:ControllerBase
    {
        /// <summary>
        /// 重启
        /// </summary>
        [HttpGet("restart")]
        public IActionResult Restart()
        {
            Startup.Restart();
            return Ok();
        }

        /// <summary>
        /// 查看状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return new JsonResult(Startup.GetStatus());
        }

        /// <summary>
        /// 填充遗漏数据
        /// </summary>
        [HttpGet("fillEmpty")]
        public IActionResult FillEmpty([FromQuery]DateTime startTime,[FromQuery]int hours)
        {
            Startup.FillEmpty?.Invoke(startTime, hours);
            return Ok();
        }
    }
}
