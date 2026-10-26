using Psdr.Dals.Configuration;
using System.Collections.Generic;

namespace Psdr.Dals.Report
{
    /// <summary>
    /// 提供库存汇总报表数据。
    /// </summary>
    public sealed class InventoryReportDAL
    {
        private readonly TenantDatabase database;
        /// <summary>
        /// 保存当前代理库连接服务。
        /// </summary>
        public InventoryReportDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 按仓库和产品汇总当前可用库存，同时返回可阅读的名称。
        /// </summary>
        public List<InventoryReportItem> GetInventorySummary()
        {
            using (var db = database.Open())
            {
                return db.Ado.SqlQuery<InventoryReportItem>(@"
                    SELECT s.StoreHouseID, h.Name AS StoreHouseName, s.ProductID,
                           p.ProductName, SUM(ISNULL(s.Amount, 0)) AS Amount
                    FROM dbo.Stock s
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID = s.StoreHouseID
                    LEFT JOIN dbo.Product p ON p.ProductID = s.ProductID
                    GROUP BY s.StoreHouseID, h.Name, s.ProductID, p.ProductName
                    ORDER BY h.Name, p.ProductName");
            }
        }
    }

    /// <summary>
    /// 库存汇总报表中的一行数据。
    /// </summary>
    public sealed class InventoryReportItem
    {
        public int? StoreHouseID { get; set; }
        public string StoreHouseName { get; set; }
        public int? ProductID { get; set; }
        public string ProductName { get; set; }
        public int Amount { get; set; }
    }
}
