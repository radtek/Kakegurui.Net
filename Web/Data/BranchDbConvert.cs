using System;
using System.Linq;
using Kakegurui.Core;
using Microsoft.EntityFrameworkCore;

namespace Kakegurui.Web.Data
{
    /// <summary>
    /// 分表处理
    /// </summary>
    public static class BranchDbConvert
    {
        /// <summary>
        /// 分表的时间级别
        /// </summary>
        public static DateTimeLevel DateLevel { get; } = DateTimeLevel.Month;

        /// <summary>
        /// 获取时间点的表名
        /// </summary>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>时间点的表名</returns>
        public static string GetTableName(DateTime baseTimePoint)
        {
            return baseTimePoint.ToString("yyyyMM");
        }

        /// <summary>
        /// 获取时间点的sql
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="baseTimePoint">基准时间点</param>
        /// <returns>时间点的sql</returns>
        private static string GetSql(string tableName, DateTime baseTimePoint)
        {
            return $"SELECT * FROM {tableName}_{GetTableName(baseTimePoint)}";
        }

        /// <summary>
        /// 根据起止时间获取是否需要分表查询
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="queryable">查询方式</param>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public static Tuple<IQueryable<T>, IQueryable<T>> GetQuerables<T>(DateTime startTime, DateTime endTime, IQueryable<T> queryable, string tableName)
            where T:class
        {
            DateTime startTimePoint = TimePointConvert.CurrentTimePoint(DateLevel,startTime);
            DateTime endTimePoint = TimePointConvert.CurrentTimePoint(DateLevel, endTime);
            DateTime currentTimePoint = TimePointConvert.CurrentTimePoint(DateLevel);
            if (startTimePoint == endTimePoint)
            {
                return new Tuple<IQueryable<T>, IQueryable<T>>(
                    startTimePoint == currentTimePoint
                        ? queryable
                        : queryable.FromSql(GetSql(tableName, startTime)), null);
            }
            else
            {
                return new Tuple<IQueryable<T>, IQueryable<T>>(
                    startTimePoint == currentTimePoint
                        ? queryable
                        : queryable.FromSql(GetSql(tableName, startTime)),
                    endTimePoint == currentTimePoint
                        ? queryable
                        : queryable.FromSql(GetSql(tableName, endTimePoint)));
            }
        }
    }
}
