using Psdr.Dals.Configuration;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Stock
{
    /// <summary>待审核销售订单的列表显示项。</summary>
    public sealed class SalesOrderReviewItem
    {
        public string No { get; set; }
        public DateTime? Time { get; set; }
        public string StoreHouseName { get; set; }
        public string ShopName { get; set; }
        public string ProductName { get; set; }
        public int? Amount { get; set; }
        public decimal? TotalMoney { get; set; }
        public decimal? OtherMoney { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>读取尚未影响库存的销售订单草稿。</summary>
    public sealed class SalesOrderReviewDAL
    {
        private readonly TenantDatabase database;

        /// <summary>保存当前代理库连接服务。</summary>
        public SalesOrderReviewDAL(TenantDatabase database) { this.database = database; }

        /// <summary>按创建时间倒序读取所有待审核销售订单。</summary>
        public List<SalesOrderReviewItem> GetPending()
        {
            using (var db = database.Open())
                return db.Ado.SqlQuery<SalesOrderReviewItem>(@"
                    SELECT i.No, i.Time, h.StoreHouseName, s.ShopName, i.ProductName, i.Amount,
                           i.TotalMoney, i.OtherMoney, i.Remark
                    FROM dbo.Indent i
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID=i.StoreHouseID
                    LEFT JOIN dbo.Shop s ON s.ShopID=i.ShopID
                    WHERE i.State=0
                    ORDER BY i.Time DESC, i.No DESC");
        }
    }
}
