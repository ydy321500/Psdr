using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Market
{
    /// <summary>维护代理在市场端展示的产品。</summary>
    public sealed class MarketProductDAL
    {
        private readonly TenantDatabase database;
        /// <summary>保存当前代理库连接服务。</summary>
        public MarketProductDAL(TenantDatabase database) { this.database = database; }
        /// <summary>查询全部市场产品。</summary>
        public List<M_Product> GetAll() { using (var db = database.Open()) return db.Queryable<M_Product>().OrderBy(x => x.ID, OrderByType.Desc).ToList(); }
        /// <summary>新增或更新市场产品，并确认关联产品存在。</summary>
        public int Save(M_Product item)
        {
            if (item == null || item.ProductID <= 0 || !item.Price.HasValue || item.Price < 0 ||
                (item.ProductType != 0 && item.ProductType != 1))
                throw new InvalidOperationException("产品、市场价格和展示类型必须有效。");
            using (var db = database.Open())
            {
                if (!db.Queryable<Psdr.Models.Entities.Product>().Any(x => x.ProductID == item.ProductID)) throw new InvalidOperationException("关联产品不存在。");
                if (item.ID == 0)
                {
                    var id = db.Insertable(item).ExecuteReturnIdentity();
                    if (id <= 0) throw new InvalidOperationException("市场产品保存失败。");
                    return id;
                }
                if (db.Updateable(item).ExecuteCommand() != 1) throw new InvalidOperationException("市场产品保存失败。");
                return item.ID;
            }
        }
        /// <summary>删除市场产品展示记录，不影响本地产品资料。</summary>
        public bool Delete(int id) { using (var db = database.Open()) return db.Deleteable<M_Product>().Where(x => x.ID == id).ExecuteCommand() == 1; }
    }
}
