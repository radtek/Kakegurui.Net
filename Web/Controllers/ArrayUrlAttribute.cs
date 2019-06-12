using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kakegurui.Web.Controllers
{
    /// <summary>
    /// 数组url参数转换
    /// </summary>
    public class ArrayUrlAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 参数名
        /// </summary>
        private readonly string _parameterName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        public ArrayUrlAttribute(string parameterName)
        {
            _parameterName = parameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            if (actionContext.ActionArguments.ContainsKey(_parameterName))
            {
                if (actionContext.RouteData.Values.ContainsKey(_parameterName))
                {
                    actionContext.ActionArguments[_parameterName] =
                        actionContext.RouteData.Values[_parameterName]
                            .ToString()
                            .Split(',')
                            .Select(int.Parse)
                            .ToArray();
                }
            }
        }
    }
}
