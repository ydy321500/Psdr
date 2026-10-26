using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Psdr.Dals.Product
{
    /// <summary>
    /// 维护当前代理库的产品资料，并校验产品类型和关联业务数据。
    /// </summary>
    public sealed class ProductDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public ProductDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 查询当前代理库的全部产品资料。
        /// </summary>
        public List<Psdr.Models.Entities.Product> GetAll()
        {
            using (var db = database.Open())
                return db.Queryable<Psdr.Models.Entities.Product>()
                    .OrderBy(x => x.ProductID, OrderByType.Desc)
                    .ToList();
        }

        /// <summary>
        /// 查询当前代理已启用且未到期的产品类型。
        /// </summary>
        public List<TenantFunction> GetActiveTypes()
        {
            using (var db = database.Open())
                return db.Queryable<TenantFunction>()
                    .Where(x => x.FunctionType == 0 && x.State == 1 && x.StopTime >= DateTime.Now)
                    .OrderBy(x => x.FunctionName)
                    .ToList();
        }

        /// <summary>
        /// 查询指定产品类型中已启用的属性定义。
        /// </summary>
        public List<Psdr.Models.Entities.ProductMap> GetEnabledFields(string productType)
        {
            using (var db = database.Open())
            {
                RequireActiveType(db, productType);
                return db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .Where(x => x.ProductName == productType && x.State == 1 && x.ValueName != "")
                    .OrderBy(x => x.KeyName)
                    .ToList();
            }
        }

        /// <summary>
        /// 新增或更新产品资料，并确认其产品类型仍可使用。
        /// </summary>
        public bool Save(Psdr.Models.Entities.Product item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            using (var db = database.Open())
            {
                RequireActiveType(db, item.ProductName);
                return item.ProductID == 0
                    ? db.Insertable(item).ExecuteCommand() == 1
                    : db.Updateable(item).ExecuteCommand() == 1;
            }
        }

        /// <summary>
        /// 删除未被库存或业务单据引用的产品资料。
        /// </summary>
        public bool Delete(int productId)
        {
            using (var db = database.Open())
            {
                if (db.Queryable<Psdr.Models.Entities.Stock>().Any(x => x.ProductID == productId) ||
                    db.Queryable<IndentInfo>().Any(x => x.ProductID == productId) ||
                    db.Queryable<ReturnInfo>().Any(x => x.ProductID == productId) ||
                    db.Queryable<StorehouseRecordInfo>().Any(x => x.ProductID == productId))
                    throw new InvalidOperationException("该产品已被库存或业务单据引用，不能删除。");

                return db.Deleteable<Psdr.Models.Entities.Product>()
                    .Where(x => x.ProductID == productId)
                    .ExecuteCommand() == 1;
            }
        }

        /// <summary>
        /// 确认产品类型存在、启用且尚未到期。
        /// </summary>
        private static void RequireActiveType(SqlSugarClient db, string productType)
        {
            if (string.IsNullOrWhiteSpace(productType) || !db.Queryable<TenantFunction>()
                .Any(x => x.FunctionName == productType && x.FunctionType == 0 && x.State == 1
                    && x.StopTime >= DateTime.Now))
                throw new InvalidOperationException("请选择已启用且未到期的产品类型。");
        }
    }
}
