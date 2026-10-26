using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System.Collections.Generic;
using SqlSugar;

namespace Psdr.Dals.Set
{
    /// <summary>维护代理库的系统配置项。</summary>
    public sealed class IniDAL
    {
        private readonly TenantDatabase database;
        /// <summary>保存当前代理库连接服务。</summary>
        public IniDAL(TenantDatabase database) { this.database = database; }
        /// <summary>读取全部系统配置项。</summary>
        public List<Ini> GetAll() { using (var db = database.Open()) return db.Queryable<Ini>().OrderBy(x => x.Type).OrderBy(x => x.Title).ToList(); }
        /// <summary>新增或更新一条系统配置项。</summary>
        public bool Save(Ini value) { using (var db = database.Open()) return value.IniID == 0 ? db.Insertable(value).ExecuteCommand() == 1 : db.Updateable(value).ExecuteCommand() == 1; }
    }
}
