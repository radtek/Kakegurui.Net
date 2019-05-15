using System;

namespace Kakegurui.Core
{
    /// <summary>
    /// 日期级别
    /// </summary>
    public enum DateTimeLevel
    {
        None,
        Minute,
        FiveMinutes,
        FifteenMinutes,
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
            if (level == DateTimeLevel.Minute)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, baseTimePoint.Minute, 0);
            }
            else if (level == DateTimeLevel.FiveMinutes)
            {
                int minutes = baseTimePoint.Minute / 5 * 5;
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, minutes, 0);
            }
            else if (level == DateTimeLevel.FifteenMinutes)
            {
                int minutes = baseTimePoint.Minute / 15 * 15;
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, minutes, 0);
            }
            else if (level == DateTimeLevel.Hour)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, baseTimePoint.Day, baseTimePoint.Hour, 0, 0);
            }
            else if (level == DateTimeLevel.Day)
            {
                return baseTimePoint.Date;
            }
            else if (level == DateTimeLevel.Month)
            {
                return new DateTime(baseTimePoint.Year, baseTimePoint.Month, 1);
            }
            else if(level==DateTimeLevel.Year)
            {
                return new DateTime(baseTimePoint.Year, 1, 1);
            }
            else
            {
                return DateTime.MinValue;
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
            if (level == DateTimeLevel.Minute)
            {
                return baseTimePoint.AddMinutes(1);
            }
            else if (level == DateTimeLevel.FiveMinutes)
            {
                return baseTimePoint.AddMinutes(5);
            }
            else if (level == DateTimeLevel.FifteenMinutes)
            {
                return baseTimePoint.AddMinutes(15);
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
            else if(level==DateTimeLevel.Year)
            {
                return baseTimePoint.AddYears(1);
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取时间格式
        /// </summary>
        /// <param name="level">时间级别</param>
        /// <returns>时间格式字符串</returns>
        public static string TimeFormat(DateTimeLevel level)
        {
            if (level == DateTimeLevel.Minute
                || level == DateTimeLevel.FiveMinutes
                || level == DateTimeLevel.FifteenMinutes)
            {
                return "yyyy-MM-dd HH:mm";
            }
            else if (level == DateTimeLevel.Hour)
            {
                return "yyyy-MM-dd HH";
            }
            else if (level == DateTimeLevel.Day)
            {
                return "yyyy-MM-dd";
            }
            else if (level == DateTimeLevel.Month)
            {
                return "yyyy-MM";
            }
            else if(level==DateTimeLevel.Year)
            {
                return "yyyy";
            }
            else
            {
                return null;
            }
        }
    }
}
