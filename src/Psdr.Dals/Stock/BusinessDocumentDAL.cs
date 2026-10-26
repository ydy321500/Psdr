using Psdr.Dals.Configuration;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Stock
{
    /// <summary>业务单据列表的统一显示模型。</summary>
    public sealed class BusinessDocumentListItem
    {
        public string DocumentType { get; set; }
        public string No { get; set; }
        public DateTime? Time { get; set; }
        public int? StoreHouseID { get; set; }
        public string StoreHouseName { get; set; }
        public string PartnerName { get; set; }
        public string ProductName { get; set; }
        public int? Amount { get; set; }
        public decimal? TotalMoney { get; set; }
        public decimal? OtherMoney { get; set; }
        public string Remark { get; set; }
        public int? State { get; set; }
        /// <summary>将销售订单状态转换为可直接识别的业务文字。</summary>
        public string StateName
        {
            get
            {
                if (DocumentType != "销售出库") return "已完成";
                return State == 0 ? "待审核" : State == 1 ? "已完成" : State == 2 ? "已作废" : "未知";
            }
        }
    }

    /// <summary>业务单据中一项产品明细的统一显示模型。</summary>
    public sealed class BusinessDocumentLineItem
    {
        public int? ProductID { get; set; }
        public string ProductType { get; set; }
        public string ProductAttribute1 { get; set; }
        public string ProductAttribute2 { get; set; }
        public int? Amount { get; set; }
        public decimal? UnitPrice { get; set; }
        public string BarCode { get; set; }
    }

    /// <summary>查询采购入库、销售出库和客户退货的业务单据。</summary>
    public sealed class BusinessDocumentDAL
    {
        private readonly TenantDatabase database;

        /// <summary>保存当前代理库连接服务。</summary>
        public BusinessDocumentDAL(TenantDatabase database) { this.database = database; }

        /// <summary>按时间倒序读取三类已经保存的业务单据。</summary>
        public List<BusinessDocumentListItem> GetAll()
        {
            using (var db = database.Open())
                return db.Ado.SqlQuery<BusinessDocumentListItem>(@"
                    SELECT N'采购入库' AS DocumentType, r.No, r.Time, r.StoreHouseID, h.StoreHouseName,
                           s.SupplyName AS PartnerName, r.ProductName, r.Amount, r.TotalMoney, r.OtherMoney, r.Remark, r.State
                    FROM dbo.StorehouseRecord r LEFT JOIN dbo.Supply s ON s.SupplyID = r.SupplyID
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID = r.StoreHouseID
                    UNION ALL
                    SELECT N'销售出库', i.No, i.Time, i.StoreHouseID, h.StoreHouseName,
                           sh.ShopName, i.ProductName, i.Amount, i.TotalMoney, i.OtherMoney, i.Remark, i.State
                    FROM dbo.Indent i LEFT JOIN dbo.Shop sh ON sh.ShopID = i.ShopID
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID = i.StoreHouseID
                    UNION ALL
                    SELECT N'客户退货', r.No, r.Time, r.StoreHouseID, h.StoreHouseName,
                           sh.ShopName, r.ProductName, r.Amount, r.TotalMoney, r.OtherMoney, r.Remark, r.State
                    FROM dbo.[Return] r LEFT JOIN dbo.Shop sh ON sh.ShopID = r.ShopID
                    LEFT JOIN dbo.StoreHouse h ON h.StoreHouseID = r.StoreHouseID
                    ORDER BY Time DESC, No DESC");
        }

        /// <summary>按单据类型和单号读取产品明细，用于追溯具体出入库项目。</summary>
        public List<BusinessDocumentLineItem> GetLines(string documentType, string number)
        {
            if (string.IsNullOrWhiteSpace(number)) return new List<BusinessDocumentLineItem>();
            string sql;
            switch (documentType)
            {
                case "采购入库":
                    sql = @"SELECT i.ProductID, p.ProductName AS ProductType, p.K1 AS ProductAttribute1, p.K2 AS ProductAttribute2,
                                   i.Amount, i.ThisPrice AS UnitPrice, CAST(NULL AS nvarchar(200)) AS BarCode
                            FROM dbo.StorehouseRecordInfo i LEFT JOIN dbo.Product p ON p.ProductID=i.ProductID WHERE i.No=@no";
                    break;
                case "销售出库":
                    sql = @"SELECT i.ProductID, p.ProductName AS ProductType, p.K1 AS ProductAttribute1, p.K2 AS ProductAttribute2,
                                   i.Amount, i.ThisPrice AS UnitPrice, i.BarCode
                            FROM dbo.IndentInfo i LEFT JOIN dbo.Product p ON p.ProductID=i.ProductID WHERE i.No=@no";
                    break;
                case "客户退货":
                    sql = @"SELECT i.ProductID, p.ProductName AS ProductType, p.K1 AS ProductAttribute1, p.K2 AS ProductAttribute2,
                                   i.Amount, i.ThisPrice AS UnitPrice, i.BarCode
                            FROM dbo.ReturnInfo i LEFT JOIN dbo.Product p ON p.ProductID=i.ProductID WHERE i.No=@no";
                    break;
                default: return new List<BusinessDocumentLineItem>();
            }
            using (var db = database.Open())
                return db.Ado.SqlQuery<BusinessDocumentLineItem>(sql, new SugarParameter("@no", number));
        }
    }
}
