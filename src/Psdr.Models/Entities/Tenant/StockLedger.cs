using SqlSugar;
using System;

namespace Psdr.Models.Entities
{
    /// <summary>
    /// 记录一次不可变的库存变更，用于追溯库存余额的来源。
    /// </summary>
    [SugarTable("StockLedger")]
    public sealed class StockLedger
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long LedgerID { get; set; }
        public string BusinessNo { get; set; }
        public string BusinessType { get; set; }
        public int StoreHouseID { get; set; }
        public int ProductID { get; set; }
        public string BarCode { get; set; }
        public int ChangeAmount { get; set; }
        public int BalanceAfter { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Remark { get; set; }
    }
}
