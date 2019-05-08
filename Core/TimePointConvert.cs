using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日期级别
    /// </summary>
    public enum DateTimeLevel
    {
        Minute,
        Second,
        Hour,
        Day,
        Month,
        Year
    }

    /// <summary>
    /// 时间处理类
    /// </summary>
    public static class TimePointConvert
    {
        /// <summary>
        /// 获取当前时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <returns>当前时间点</returns>
        public static DateTime CurrentTimePoint(DateTimeLevel level)
        {
            return CurrentTimePoint(level, DateTime.Now);
        }

        /// <summary>
        /// 获取当前时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>当前时间点</returns>
        public static DateTime CurrentTimePoint(DateTimeLevel level,DateTime baseTimePoint)
        {
            if (level == DateTimeLevel.Second)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, baseTimePoint.Minute, baseTimePoint.Second);
            }
            else if (level == DateTimeLevel.Minute)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, baseTimePoint.Minute, 0);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, 0, 0);
            }
            else if (level == DateTimeLevel.Day)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day);
            }
            else if (level == DateTimeLevel.Month)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, 1);
            }
            else
            {
                return new DateTime(baseTimePoint.Year, 1, 1);
            }
        }

        /// <summary>
        /// 获取下一个时间点
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>下一个时间点</returns>
        public static DateTime NextTimePoint(DateTimeLevel level, DateTime baseTimePoint)
        {
            if (level == DateTimeLevel.Second)
            {
                return baseTimePoint.AddSeconds(1);
            }
            else if (level == DateTimeLevel.Minute)
            {
                return baseTimePoint.AddMinutes(1);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return baseTimePoint.AddHours(1);
            }
            else if (level == DateTimeLevel.Day)
            {
                return baseTimePoint.AddDays(1);
            }
            else if (level == DateTimeLevel.Month)
            {
                return baseTimePoint.AddMonths(1);
            }
            else
            {
                return baseTimePoint.AddYears(1);
            }
        }
    }
}
