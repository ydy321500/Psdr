using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Market
{
    /// <summary>
    /// 查询并维护市场端提交到代理库的订单状态。
    /// </summary>
    public sealed class MarketIndentDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存当前代理库连接服务。
        /// </summary>
        public MarketIndentDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 读取市场端订单，并关联产品名称用于后台审核。
        /// </summary>
        public List<MarketIndentItem> GetAll()
        {
            using (var db = database.Open())
            {
                return db.Ado.SqlQuery<MarketIndentItem>(@"
                    SELECT m.IndentID, m.IndentNo, m.ProductID, p.ProductName, m.Amount, m.State
                    FROM dbo.M_Indent m
                    LEFT JOIN dbo.Product p ON p.ProductID = m.ProductID
                    ORDER BY m.IndentID DESC");
            }
        }

        /// <summary>
        /// 修改市场订单状态；状态含义由市场端约定，桌面端只负责审核流转。
        /// </summary>
        public void SetState(int indentId, int state)
        {
            if (indentId <= 0) throw new ArgumentOutOfRangeException(nameof(indentId));
            if (state < 0 || state > 2) throw new ArgumentOutOfRangeException(nameof(state));
            using (var db = database.Open())
            {
                if (db.Updateable<M_Indent>().SetColumns(x => x.State == state)
                    .Where(x => x.IndentID == indentId).ExecuteCommand() != 1)
                    throw new InvalidOperationException("市场订单不存在。");
            }
        }
    }

    /// <summary>
    /// 市场订单列表项。
    /// </summary>
    public sealed class MarketIndentItem
    {
        public int IndentID { get; set; }
        public string IndentNo { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public int Amount { get; set; }
        public int State { get; set; }
        public string StateName => State == 0 ? "待处理" : State == 1 ? "已确认" : "已关闭";
    }
}
