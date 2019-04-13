using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 时间戳转换
    /// </summary>
    public static class TimeStampConvert
    {
        /// <summary>
        /// 获取指定时间的时间戳
        /// </summary>
        /// <param name="dateTime">时间</param>
        /// <returns>时间戳</returns>
        public static long ToTimeStamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime- new DateTime(1970, 1, 1)).TotalMilliseconds);
        }

        /// <summary>
        /// 获取指定时间转换为utc时间的时间戳
        /// </summary>
        /// <param name="dateTime">时间</param>
        /// <returns>时间戳</returns>
        public static long ToUtcTimeStamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds);
        }

        /// <summary>
        /// 获取当前utc时间的时间戳
        /// </summary>
        /// <returns>时间戳</returns>
        public static long ToUtcTimeStamp()
        {
            return Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds);
        }
     
        /// <summary>
        /// 获取指定时间戳的时间
        /// </summary>
        /// <param name="timeStamp">时间戳</param>
        /// <returns>时间</returns>
        public static DateTime ToDateTime(long timeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1);
            return dateTime.AddMilliseconds(timeStamp);
        }

        /// <summary>
        /// 获取指定时间戳的本地时间
        /// </summary>
        /// <param name="timeStamp">时间戳</param>
        /// <returns>时间</returns>
        public static DateTime ToLocalDateTime(long timeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1);
            return dateTime.AddMilliseconds(timeStamp).ToLocalTime();
        }
    }
}
