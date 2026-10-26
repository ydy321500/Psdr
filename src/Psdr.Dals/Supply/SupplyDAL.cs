using System.Collections.Generic;
using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;

namespace Psdr.Dals.Supply
{
        /// <summary>Maintains suppliers in the current tenant database.</summary>
        public sealed class SupplyDAL
        {
        private readonly TenantDatabase database;
        
        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public SupplyDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
            /// 按编号倒序查询当前代理的全部供应商资料。
        /// </summary>
        public List<Psdr.Models.Entities.Supply> GetAll()
        {
            using (var db = database.Open())
                return db.Queryable<Psdr.Models.Entities.Supply>()
                    .OrderBy(x => x.SupplyID, OrderByType.Desc).ToList();
        }

        /// <summary>
            /// 新增或更新供应商资料，并拒绝同名供应商。
        /// </summary>
        public bool Save(Psdr.Models.Entities.Supply item)
        {
            using (var db = database.Open())
            {
                if (db.Queryable<Psdr.Models.Entities.Supply>()
                    .Any(x => x.SupplyName == item.SupplyName && x.SupplyID != item.SupplyID)) return false;
                return item.SupplyID == 0
                    ? db.Insertable(item).ExecuteCommand() == 1
                    : db.Updateable(item).ExecuteCommand() == 1;
            }
        }

        /// <summary>
            /// 按供应商编号删除一条供应商资料。
        /// </summary>
        public bool Delete(int id)
        {
            using (var db = database.Open())
                return db.Deleteable<Psdr.Models.Entities.Supply>()
                    .Where(x => x.SupplyID == id).ExecuteCommand() == 1;
        }
    }
}
