using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System.Collections.Generic;

namespace Psdr.Dals.Set
{
    /// <summary>查询代理库中的操作日志。</summary>
    public sealed class LogDAL
    {
        private readonly TenantDatabase database;
        /// <summary>保存当前代理库连接服务。</summary>
        public LogDAL(TenantDatabase database) { this.database = database; }
        /// <summary>按记录时间倒序读取最近操作日志。</summary>
        public List<Log> GetRecent(int limit)
        {
            if (limit <= 0) limit = 500;
            using (var db = database.Open()) return db.Queryable<Log>().OrderBy(x => x.LogTime, OrderByType.Desc).Take(limit).ToList();
        }
    }
}
