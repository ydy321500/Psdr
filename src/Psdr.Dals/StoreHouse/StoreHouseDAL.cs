using System.Collections.Generic;
using SqlSugar;
using Psdr.Dals.Configuration;
using Psdr.Models.Entities;

namespace Psdr.Dals.StoreHouse
{
    /// <summary>
    /// Performs warehouse maintenance against the current tenant database.
    /// </summary>
    public sealed class StoreHouseDAL
    {
        private readonly TenantDatabase tenantDatabase;

        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public StoreHouseDAL(TenantDatabase tenantDatabase)
        {
            this.tenantDatabase = tenantDatabase;
        }

        /// <summary>
            /// 按编号倒序查询当前代理的全部仓库资料。
        /// </summary>
        public List<Psdr.Models.Entities.StoreHouse> GetAll()
        {
            using (var db = tenantDatabase.Open())
            {
                return db.Queryable<Psdr.Models.Entities.StoreHouse>()
                    .OrderBy(x => x.StoreHouseID, OrderByType.Desc)
                    .ToList();
            }
        }

        /// <summary>
            /// 新增或更新仓库资料，并拒绝同名仓库。
        /// </summary>
        public bool Save(Psdr.Models.Entities.StoreHouse item)
        {
            using (var db = tenantDatabase.Open())
            {
                var exists = db.Queryable<Psdr.Models.Entities.StoreHouse>()
                    .Any(x => x.Name == item.Name && x.StoreHouseID != item.StoreHouseID);
                if (exists)
                {
                    return false;
                }

                if (item.StoreHouseID == 0)
                {
                    return db.Insertable(item).ExecuteCommand() == 1;
                }

                return db.Updateable(item).ExecuteCommand() == 1;
            }
        }

        /// <summary>
            /// 按仓库编号删除一条仓库资料。
        /// </summary>
        public bool Delete(int id)
        {
            using (var db = tenantDatabase.Open())
            {
                return db.Deleteable<Psdr.Models.Entities.StoreHouse>()
                    .Where(x => x.StoreHouseID == id)
                    .ExecuteCommand() == 1;
            }
        }
    }
}
