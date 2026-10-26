using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Admin
{
    /// <summary>
    /// 查询主库中由管理端和服务端写入的审计日志。
    /// </summary>
    public sealed class AdminLogDAL
    {
        private readonly CentralDatabase database;

        /// <summary>
        /// 保存主库连接服务。
        /// </summary>
        public AdminLogDAL(CentralDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 读取最近主库审计日志。
        /// </summary>
        public List<ServerLog> GetRecent(int limit)
        {
            if (limit <= 0) limit = 500;
            using (var db = database.OpenClient())
            {
                return db.Queryable<ServerLog>().OrderBy(x => x.LogID, OrderByType.Desc).Take(limit).ToList();
            }
        }

        /// <summary>
        /// 写入管理端操作审计记录，不包含密码或数据库连接凭据。
        /// </summary>
        public void Write(string userName, string eventName, string info)
        {
            using (var db = database.OpenClient())
            {
                db.Insertable(new ServerLog
                {
                    UserName = string.IsNullOrWhiteSpace(userName) ? "管理员" : userName,
                    MachineInfo = Environment.MachineName,
                    Event = eventName,
                    Info = info,
                    LogTime = DateTime.Now,
                    State = 1
                }).ExecuteCommand();
            }
        }
    }
}
