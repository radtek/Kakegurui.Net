using System.Linq;
using System.Threading.Tasks;
using Kakegurui.Web.Models.Device;
using Kakegurui.Web.Data;
using Kakegurui.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kakegurui.Web.Controllers
{
    /// <summary>
    /// 路口
    /// </summary>
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class RoadsController : ControllerBase
    {
        /// <summary>
        /// 数据库实例
        /// </summary>
        private readonly DeviceContext _context;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="context">数据库实例</param>
        public RoadsController(DeviceContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 查询路口集合
        /// </summary>
        /// <param name="name">路口名称</param>
        /// <param name="pageNum">页码</param>
        /// <param name="pageSize">分页数量</param>
        /// <returns>查询结果</returns>
        [HttpGet]
        public PageModel<TrafficRoad> GetRoads([FromQuery] string name, [FromQuery] int pageNum, [FromQuery] int pageSize)
        {
            IQueryable<TrafficRoad> queryable = _context.Roads;

            if (!string.IsNullOrEmpty(name))
            {
                queryable = queryable.Where(d => d.Name.Contains(name));
            }

            PageModel<TrafficRoad> model = new PageModel<TrafficRoad>
            {
                Total = queryable.Count()
            };

            if (pageNum != 0 && pageSize != 0)
            {
                queryable = queryable
                    .Skip((pageNum - 1) * pageSize)
                    .Take(pageSize);
            }
            model.Datas = queryable.ToList();
            return model;
        }

        /// <summary>
        /// 查询路口
        /// </summary>
        /// <param name="id">路口编号</param>
        /// <returns>查询结果</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoad([FromRoute] int id)
        {
            TrafficRoad road = await _context.Roads.SingleOrDefaultAsync(c => c.Id == id);

            if (road == null)
            {
                return NotFound();
            }

            return Ok(road);
        }

        /// <summary>
        /// 添加路口
        /// </summary>
        /// <param name="road">路口</param>
        /// <returns>添加结果</returns>
        [HttpPost]
        public async Task<IActionResult> PostRoad([FromBody] TrafficRoad road)
        {
            try
            {
                _context.Roads.Add(road);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (_context.Roads.Count(d => d.Id == road.Id) > 0)
                {
                    return new StatusCodeResult(StatusCodes.Status409Conflict);
                }
                else
                {
                    throw;
                }
            }
            return Ok(road);
        }

        /// <summary>
        /// 更新路口
        /// </summary>
        /// <param name="road">路口</param>
        /// <returns>更新结果</returns>
        [HttpPut]
        public async Task<IActionResult> PutRoad([FromBody] TrafficRoad road)
        {

            _context.Entry(road).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (_context.Roads.Count(d => d.Id == road.Id) == 0)
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok();
        }

        /// <summary>
        /// 删除路口
        /// </summary>
        /// <param name="id">路口编号</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoad([FromRoute] int id)
        {
            TrafficRoad road = await _context.Roads.SingleOrDefaultAsync(d => d.Id == id);
            if (road == null)
            {
                return NotFound();
            }

            _context.Roads.Remove(road);
            await _context.SaveChangesAsync();
            return Ok(road);
        }
    }
}
