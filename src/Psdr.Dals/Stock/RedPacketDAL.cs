using Psdr.Dals.Configuration;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Stock
{
    /// <summary>
    /// 查询已售条码及其红包兑换状态。
    /// </summary>
    public sealed class RedPacketDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存当前代理库连接服务。
        /// </summary>
        public RedPacketDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 读取最近售出条码及对应的红包兑换状态。
        /// </summary>
        public List<RedPacketSaleItem> GetRecent(int limit)
        {
            if (limit <= 0) limit = 500;
            using (var db = database.Open())
            {
                return db.Ado.SqlQuery<RedPacketSaleItem>(@"
                    SELECT TOP " + limit + @" r.RedPacketID, r.BarCode, p.ProductName, s.ShopName,
                           r.UpdateTime AS SoldAt, q.Money AS RedeemedMoney, q.InsertTime AS RequestedAt,
                           q.UpdateTime AS RedeemedAt,
                           CASE WHEN q.RedPackeRequestID IS NULL THEN N'未兑换'
                                WHEN q.UpdateTime IS NULL THEN N'兑换申请中'
                                ELSE N'已兑换' END AS StateName
                    FROM dbo.RedPacketList r
                    LEFT JOIN dbo.Product p ON p.ProductID = r.ProductID
                    LEFT JOIN dbo.Shop s ON s.ShopID = r.ShopID
                    LEFT JOIN dbo.RedPacketRequestList q ON q.RedPackeRequestID =
                    (
                        SELECT TOP 1 q2.RedPackeRequestID
                        FROM dbo.RedPacketRequestList q2
                        WHERE q2.BarCode = r.BarCode
                        ORDER BY q2.RedPackeRequestID DESC
                    )
                    ORDER BY r.RedPacketID DESC");
            }
        }
    }

    /// <summary>
    /// 已售条码及红包兑换状态的一行展示数据。
    /// </summary>
    public sealed class RedPacketSaleItem
    {
        public long RedPacketID { get; set; }
        public string BarCode { get; set; }
        public string ProductName { get; set; }
        public string ShopName { get; set; }
        public DateTime? SoldAt { get; set; }
        public decimal? RedeemedMoney { get; set; }
        public DateTime? RequestedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public string StateName { get; set; }
    }
}
