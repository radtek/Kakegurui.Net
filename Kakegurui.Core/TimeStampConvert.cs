using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 时间戳转换
    /// </summary>
    public class TimeStampConvert
    {
        /// <summary>
        /// 获取当前时间的时间戳
        /// </summary>
        /// <returns>时间戳</returns>
        public static long ToTimeStamp()
        {
            return Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
        }
        
        /// <summary>
        /// 获取指定时间的时间戳
        /// </summary>
        /// <param name="dateTime">时间</param>
        /// <returns>时间戳</returns>
        public static long ToTimeStamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime()-new DateTime(1970,1,1)).TotalMilliseconds);
        }
    }
}
