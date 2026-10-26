using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Finance
{
    /// <summary>客户和供应商财务流水的统一显示项。</summary>
    public sealed class FinanceListItem
    {
        public string PartnerType { get; set; }
        public long FinanceID { get; set; }
        public string PartnerName { get; set; }
        public decimal Money { get; set; }
        public DateTime? Time { get; set; }
        public int? Type { get; set; }
        public string No { get; set; }
        public decimal? Postage { get; set; }
        public string Remark { get; set; }
        /// <summary>指示该流水是否由采购、销售或退货业务单自动生成。</summary>
        public bool IsBusinessDocument { get; set; }
    }

    /// <summary>维护客户和供应商往来账务流水。</summary>
    public sealed class FinanceDAL
    {
        private readonly TenantDatabase database;
        /// <summary>保存当前代理库连接服务。</summary>
        public FinanceDAL(TenantDatabase database) { this.database = database; }

        /// <summary>查询全部客户和供应商往来流水。</summary>
        public List<FinanceListItem> GetAll()
        {
            using (var db = database.Open()) return db.Ado.SqlQuery<FinanceListItem>(@"
                SELECT N'客户' PartnerType, f.FinanceID, s.ShopName PartnerName, f.Money, f.Time, f.Type, f.No, f.Postage, f.Remark,
                       CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.Indent i WHERE i.No=f.No)
                                      OR EXISTS(SELECT 1 FROM dbo.[Return] r WHERE r.No=f.No) THEN 1 ELSE 0 END AS bit) IsBusinessDocument
                FROM dbo.FinanceShop f LEFT JOIN dbo.Shop s ON s.ShopID=f.ShopID
                UNION ALL
                SELECT N'供应商', f.FinanceID, s.SupplyName, f.Money, f.Time, f.Type, f.No, f.Postage, f.Remark,
                       CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.StorehouseRecord r WHERE r.No=f.No) THEN 1 ELSE 0 END AS bit)
                FROM dbo.FinanceSupply f LEFT JOIN dbo.Supply s ON s.SupplyID=f.SupplyID
                ORDER BY Time DESC, FinanceID DESC");
        }

        /// <summary>新增客户往来账务；类型 0 为记账，1 为销账。</summary>
        public void SaveShop(int shopId, decimal money, int type, string number, decimal? postage, string remark)
        {
            if (shopId <= 0 || money <= 0 || (type != 0 && type != 1)) throw new InvalidOperationException("客户、金额和账务类型必须有效。");
            using (var db = database.Open())
            {
                if (!db.Queryable<Psdr.Models.Entities.Shop>().Any(x => x.ShopID == shopId)) throw new InvalidOperationException("客户不存在。");
                db.Insertable(new FinanceShop { ShopID = shopId, Money = money, Time = DateTime.Now, Type = type, No = number, Postage = postage, State = 1, Remark = remark }).ExecuteCommand();
            }
        }

        /// <summary>新增供应商往来账务；类型 0 为记账，1 为销账。</summary>
        public void SaveSupply(int supplyId, decimal money, int type, string number, decimal? postage, string remark)
        {
            if (supplyId <= 0 || money <= 0 || (type != 0 && type != 1)) throw new InvalidOperationException("供应商、金额和账务类型必须有效。");
            using (var db = database.Open())
            {
                if (!db.Queryable<Psdr.Models.Entities.Supply>().Any(x => x.SupplyID == supplyId)) throw new InvalidOperationException("供应商不存在。");
                db.Insertable(new FinanceSupply { SupplyID = supplyId, Money = money, Time = DateTime.Now, Type = type, No = number, Postage = postage, State = 1, Remark = remark }).ExecuteCommand();
            }
        }

        /// <summary>
        /// 删除一条手工财务流水；客户和供应商记录分别从对应表删除。
        /// </summary>
        public void Delete(string partnerType, long financeId)
        {
            if (financeId <= 0) throw new ArgumentOutOfRangeException(nameof(financeId));
            using (var db = database.Open())
            {
                var shopResult = partnerType == "客户" ? db.Ado.SqlQuery<int>(@"SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.FinanceShop f
                        WHERE f.FinanceID=@id AND (EXISTS(SELECT 1 FROM dbo.Indent i WHERE i.No=f.No)
                        OR EXISTS(SELECT 1 FROM dbo.[Return] r WHERE r.No=f.No))) THEN 1 ELSE 0 END", new SqlSugar.SugarParameter("@id", financeId)) : null;
                var supplyResult = partnerType == "供应商" ? db.Ado.SqlQuery<int>(@"SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.FinanceSupply f
                            WHERE f.FinanceID=@id AND EXISTS(SELECT 1 FROM dbo.StorehouseRecord r WHERE r.No=f.No)) THEN 1 ELSE 0 END", new SqlSugar.SugarParameter("@id", financeId))
                    : null;
                if (partnerType != "客户" && partnerType != "供应商") throw new InvalidOperationException("未知的财务往来对象。");
                var isBusinessDocument = shopResult != null ? shopResult.Count > 0 && shopResult[0] == 1 : supplyResult.Count > 0 && supplyResult[0] == 1;
                if (isBusinessDocument) throw new InvalidOperationException("该流水由业务单据自动生成，请通过对应业务单据处理，不能直接删除。");
                var changed = partnerType == "客户"
                    ? db.Deleteable<FinanceShop>().Where(x => x.FinanceID == financeId).ExecuteCommand()
                    : partnerType == "供应商"
                        ? db.Deleteable<FinanceSupply>().Where(x => x.FinanceID == financeId).ExecuteCommand()
                        : 0;
                if (changed != 1) throw new InvalidOperationException("财务流水不存在。");
            }
        }
    }
}
