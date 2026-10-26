using Psdr.Dals.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Psdr.Dals.Stock
{
    /// <summary>
    /// 库存查询结果，包含库存记录及其产品显示信息。
    /// </summary>
    public sealed class StockListItem
    {
        public long StockID { get; set; }
        public int? StoreHouseID { get; set; }
        public string StoreHouseName { get; set; }
        public int? ProductID { get; set; }
        public string ProductType { get; set; }
        public string ProductAttribute1 { get; set; }
        public string ProductAttribute2 { get; set; }
        public string Position { get; set; }
        public string BarCode { get; set; }
        public int? Amount { get; set; }
    }

    /// <summary>库存流水查询结果，展示变更来源与变更后的余额。</summary>
    public sealed class StockLedgerListItem
    {
        public string BusinessNo { get; set; }
        public string BusinessType { get; set; }
        public int StoreHouseID { get; set; }
        public int ProductID { get; set; }
        public string BarCode { get; set; }
        public int ChangeAmount { get; set; }
        public int BalanceAfter { get; set; }
        public System.DateTime OccurredAt { get; set; }
    }

    /// <summary>库存余额与最近流水余额的核对结果。</summary>
    public sealed class StockReconciliationItem
    {
        public int? StoreHouseID { get; set; }
        public int? ProductID { get; set; }
        public string BarCode { get; set; }
        public int? StockAmount { get; set; }
        public int? LedgerBalance { get; set; }
        public int Difference { get; set; }
    }

    /// <summary>
    /// 有码盘点待扫描的在库条码。
    /// </summary>
    public sealed class BarcodeInventoryItem
    {
        public long StockID { get; set; }
        public string BarCode { get; set; }
        public int Amount { get; set; }
        public bool IsScanned { get; set; }
    }

    /// <summary>
    /// 查询当前代理库的实时库存，不在此层直接修改库存数量。
    /// </summary>
    public sealed class StockDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public StockDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 查询库存记录并关联产品类型、属性和库位信息。
        /// </summary>
        public List<StockListItem> GetAll()
        {
            using (var db = database.Open())
                return db.Ado.SqlQuery<StockListItem>(@"
                    SELECT s.StockID, s.StoreHouseID, h.Name AS StoreHouseName, s.ProductID, p.ProductName AS ProductType,
                           p.K1 AS ProductAttribute1, p.K2 AS ProductAttribute2,
                           p.Position, s.BarCode, s.Amount
                    FROM dbo.Stock s
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID = s.StoreHouseID
                    LEFT JOIN dbo.Product p ON p.ProductID = s.ProductID
                    ORDER BY s.StockID DESC");
        }

        /// <summary>查询最近的库存变更流水，供库存差异追溯。</summary>
        public List<StockLedgerListItem> GetRecentLedger()
        {
            using (var db = database.Open())
            {
                if (!HasStockLedger(db)) return new List<StockLedgerListItem>();
                return db.Ado.SqlQuery<StockLedgerListItem>(@"
                    SELECT TOP 200 BusinessNo, BusinessType, StoreHouseID, ProductID, BarCode,
                           ChangeAmount, BalanceAfter, OccurredAt
                    FROM dbo.StockLedger ORDER BY LedgerID DESC");
            }
        }

        /// <summary>判断当前代理库是否已包含新版库存流水表，不修改任何既有库结构。</summary>
        public bool HasStockLedger()
        {
            using (var db = database.Open()) return HasStockLedger(db);
        }

        /// <summary>按库存键对比实时余额和最后一笔库存流水余额，找出需要盘点的差异。</summary>
        public List<StockReconciliationItem> GetReconciliation()
        {
            using (var db = database.Open())
            {
                if (!HasStockLedger(db)) return new List<StockReconciliationItem>();
                return db.Ado.SqlQuery<StockReconciliationItem>(@"
                    WITH LastLedger AS
                    (
                        SELECT StoreHouseID, ProductID, ISNULL(BarCode, N'') AS BarCode, BalanceAfter,
                               ROW_NUMBER() OVER(PARTITION BY StoreHouseID, ProductID, ISNULL(BarCode, N'') ORDER BY LedgerID DESC) AS rn
                        FROM dbo.StockLedger
                    )
                    SELECT s.StoreHouseID, s.ProductID, s.BarCode, s.Amount AS StockAmount,
                           l.BalanceAfter AS LedgerBalance, ISNULL(s.Amount, 0) - ISNULL(l.BalanceAfter, 0) AS Difference
                    FROM dbo.Stock s
                    LEFT JOIN LastLedger l ON l.StoreHouseID=s.StoreHouseID AND l.ProductID=s.ProductID
                        AND l.BarCode=ISNULL(s.BarCode, N'') AND l.rn=1
                    WHERE ISNULL(s.Amount, 0) <> ISNULL(l.BalanceAfter, 0)
                    ORDER BY s.StoreHouseID, s.ProductID, s.BarCode");
            }
        }

        /// <summary>
        /// 读取指定仓库、产品下可用于扫码盘点的在库条码。
        /// </summary>
        public List<BarcodeInventoryItem> GetBarcodeInventory(int storeHouseId, int productId)
        {
            using (var db = database.Open())
            {
                return db.Ado.SqlQuery<BarcodeInventoryItem>(@"
                    SELECT StockID, BarCode, ISNULL(Amount, 0) AS Amount, CAST(0 AS bit) AS IsScanned
                    FROM dbo.Stock
                    WHERE StoreHouseID=@storeHouseId AND ProductID=@productId
                      AND ISNULL(Amount, 0) > 0 AND ISNULL(BarCode, N'') <> N''
                    ORDER BY BarCode",
                    new SqlSugar.SugarParameter("@storeHouseId", storeHouseId),
                    new SqlSugar.SugarParameter("@productId", productId));
            }
        }

        private static bool HasStockLedger(SqlSugar.SqlSugarClient db)
        {
            return db.Ado.SqlQuery<int>("SELECT CASE WHEN OBJECT_ID(N'dbo.StockLedger', N'U') IS NULL THEN 0 ELSE 1 END").FirstOrDefault() == 1;
        }
    }
}
