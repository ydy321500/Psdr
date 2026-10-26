using System.Collections.Generic;
using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;

namespace Psdr.Dals.Shop
{
        /// <summary>Maintains customers in the current tenant database.</summary>
        public sealed class ShopDAL
        {
        private readonly TenantDatabase database;
        
        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public ShopDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
            /// 按编号倒序查询当前代理的全部客户资料。
        /// </summary>
        public List<Psdr.Models.Entities.Shop> GetAll()
        {
            using (var db = database.Open())
                return db.Queryable<Psdr.Models.Entities.Shop>()
                    .OrderBy(x => x.ShopID, OrderByType.Desc).ToList();
        }

        /// <summary>
        /// 查询可供客户选择的销售人员名称列表。
        /// </summary>
        public List<string> GetSalesmanNames()
        {
            using (var db = database.Open())
                return db.Queryable<Psdr.Models.Entities.SalesMan>()
                    .OrderBy(x => x.Name).Select(x => x.Name).ToList();
        }

        /// <summary>
            /// 新增或更新客户资料，并拒绝同名客户。
        /// </summary>
        public bool Save(Psdr.Models.Entities.Shop item)
        {
            using (var db = database.Open())
            {
                if (db.Queryable<Psdr.Models.Entities.Shop>()
                    .Any(x => x.ShopName == item.ShopName && x.ShopID != item.ShopID)) return false;
                return item.ShopID == 0
                    ? db.Insertable(item).ExecuteCommand() == 1
                    : db.Updateable(item).ExecuteCommand() == 1;
            }
        }

        /// <summary>
            /// 按客户编号删除一条客户资料。
        /// </summary>
        public bool Delete(int id)
        {
            using (var db = database.Open())
                return db.Deleteable<Psdr.Models.Entities.Shop>()
                    .Where(x => x.ShopID == id).ExecuteCommand() == 1;
        }
    }
}
