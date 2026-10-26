using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Finance
{
    /// <summary>维护客户专属售价和供应商专属进价。</summary>
    public sealed class PersonPriceDAL
    {
        private readonly TenantDatabase database;

        /// <summary>保存当前代理库连接服务。</summary>
        public PersonPriceDAL(TenantDatabase database) { this.database = database; }

        /// <summary>新增或更新客户针对产品的专属售价和红包价格区间。</summary>
        public void SaveShopPrice(int shopId, long productId, string productName, decimal? price, decimal? redLow, decimal? redHigh)
        {
            if (shopId <= 0 || productId <= 0 || price < 0 || redLow < 0 || redHigh < 0) throw new InvalidOperationException("客户、产品和价格必须有效。");
            using (var db = database.Open())
            {
                var value = db.Queryable<PersonPriceShop>().First(x => x.ShopID == shopId && x.ProductID == productId);
                if (value == null) db.Insertable(new PersonPriceShop { ShopID = shopId, ProductID = productId, ProductName = productName, PersonPricesShop = price, PersonRedPacketPriceLow = redLow, PersonRedPacketPriceHigh = redHigh }).ExecuteCommand();
                else { value.ProductName = productName; value.PersonPricesShop = price; value.PersonRedPacketPriceLow = redLow; value.PersonRedPacketPriceHigh = redHigh; db.Updateable(value).UpdateColumns(x => new { x.ProductName, x.PersonPricesShop, x.PersonRedPacketPriceLow, x.PersonRedPacketPriceHigh }).ExecuteCommand(); }
            }
        }

        /// <summary>新增或更新供应商针对产品的专属进价。</summary>
        public void SaveSupplyPrice(int supplyId, long productId, string productName, decimal? price)
        {
            if (supplyId <= 0 || productId <= 0 || price < 0) throw new InvalidOperationException("供应商、产品和价格必须有效。");
            using (var db = database.Open())
            {
                var value = db.Queryable<PersonPriceSupply>().First(x => x.SupplyID == supplyId && x.ProductID == productId);
                if (value == null) db.Insertable(new PersonPriceSupply { SupplyID = supplyId, ProductID = productId, ProductName = productName, PersonPricesSupply = price }).ExecuteCommand();
                else { value.ProductName = productName; value.PersonPricesSupply = price; db.Updateable(value).UpdateColumns(x => new { x.ProductName, x.PersonPricesSupply }).ExecuteCommand(); }
            }
        }

        /// <summary>读取指定客户已设置的专属价格。</summary>
        public List<PersonPriceShop> GetShopPrices(int shopId)
        {
            using (var db = database.Open()) return db.Queryable<PersonPriceShop>().Where(x => x.ShopID == shopId).ToList();
        }

        /// <summary>读取指定供应商已设置的专属价格。</summary>
        public List<PersonPriceSupply> GetSupplyPrices(int supplyId)
        {
            using (var db = database.Open()) return db.Queryable<PersonPriceSupply>().Where(x => x.SupplyID == supplyId).ToList();
        }
    }
}
