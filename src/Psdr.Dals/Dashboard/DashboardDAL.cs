using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Dashboard
{
        /// <summary>Current tenant's business counts for the system dashboard.</summary>
        public sealed class DashboardSnapshot
        {
        public int CustomerCount { get; set; }
        public int SupplierCount { get; set; }
        public int ProductCount { get; set; }
        public int WarehouseCount { get; set; }
        public bool FunctionSchemaAvailable { get; set; }
        public List<string> ActiveProductTypes { get; set; } = new List<string>();
        }
        
        public sealed class DashboardDAL
        {
        private readonly TenantDatabase tenantDatabase;
        
        /// <summary>
            /// 保存用于读取当前代理看板数据的数据库连接服务。
        /// </summary>
        public DashboardDAL(TenantDatabase tenantDatabase)
        {
            this.tenantDatabase = tenantDatabase;
        }

        /// <summary>
        /// 查询首页看板所需的业务统计信息。
        /// </summary>
        public DashboardSnapshot GetSnapshot()
        {
            using (var db = tenantDatabase.Open())
            {
                var snapshot = new DashboardSnapshot
                {
                    CustomerCount = db.Queryable<Psdr.Models.Entities.Shop>().Count(),
                    SupplierCount = db.Queryable<Psdr.Models.Entities.Supply>().Count(),
                    ProductCount = db.Queryable<Product>().Count(),
                    WarehouseCount = db.Queryable<Psdr.Models.Entities.StoreHouse>().Count()
                };

                // 历史代理库可能缺少产品功能表；此时仍显示基础资料统计。
                var hasFunctions = Convert.ToInt32(db.Ado.GetScalar(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.TenantFunction', N'U') IS NULL THEN 0 ELSE 1 END")) != 0;
                snapshot.FunctionSchemaAvailable = hasFunctions;
                if (hasFunctions)
                {
                    snapshot.ActiveProductTypes = db.Queryable<TenantFunction>()
                        .Where(x => x.FunctionType == 0 && x.State == 1 && x.StopTime >= DateTime.Now)
                        .OrderBy(x => x.FunctionName)
                        .Select(x => x.FunctionName)
                        .ToList();
                }
                return snapshot;
            }
        }
    }
}
