using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Psdr.Dals.Stock
{
    /// <summary>描述一项待入库的产品、数量、采购单价和可选条码。</summary>
    public sealed class StockInLine
    {
        public int ProductID { get; set; }
        public int Amount { get; set; }
        public decimal UnitPrice { get; set; }
        public string BarCode { get; set; }
    }

    /// <summary>描述一次采购入库所需的单头和明细数据。</summary>
    public sealed class StockInRequest
    {
        public int StoreHouseID { get; set; }
        public int SupplyID { get; set; }
        public string ProductType { get; set; }
        public decimal OtherMoney { get; set; }
        public string Remark { get; set; }
        public List<StockInLine> Lines { get; set; } = new List<StockInLine>();
    }

    /// <summary>描述销售出库或客户退货共用的产品数量、单价和条码。</summary>
    public sealed class StockMoveLine
    {
        public int ProductID { get; set; }
        public int Amount { get; set; }
        public decimal UnitPrice { get; set; }
        public string BarCode { get; set; }
    }

    /// <summary>
    /// 描述一次盘点确认后的库存数量及其差异原因。
    /// </summary>
    public sealed class InventoryAdjustmentRequest
    {
        public int StoreHouseID { get; set; }
        public int ProductID { get; set; }
        public string BarCode { get; set; }
        public int ActualAmount { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>执行入库单、库存余额和库存流水的原子写入。</summary>
    public sealed class StockTransactionService
    {
        private readonly TenantDatabase database;
        /// <summary>保存用于访问当前代理库的数据库连接服务。</summary>
        public StockTransactionService(TenantDatabase database) { this.database = database; }

        /// <summary>创建采购入库单，并将每个明细计入指定仓库的库存余额。</summary>
        public string Receive(StockInRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.StoreHouseID <= 0 || request.SupplyID <= 0) throw new InvalidOperationException("请选择仓库和供应商。");
            if (request.Lines == null || request.Lines.Count == 0 || request.Lines.Any(x => x.ProductID <= 0 || x.Amount <= 0 || x.UnitPrice < 0))
                throw new InvalidOperationException("入库明细不能为空，且产品、数量、单价必须有效。");
            var number = "RK" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    if (!db.Queryable<Psdr.Models.Entities.StoreHouse>().Any(x => x.StoreHouseID == request.StoreHouseID) ||
                        !db.Queryable<Psdr.Models.Entities.Supply>().Any(x => x.SupplyID == request.SupplyID))
                        throw new InvalidOperationException("仓库或供应商不存在。");
                    var header = new StorehouseRecord
                    {
                        No = number, StoreHouseID = request.StoreHouseID, SupplyID = request.SupplyID, ProductName = request.ProductType,
                        Time = DateTime.Now, Amount = request.Lines.Sum(x => x.Amount), TotalMoney = request.Lines.Sum(x => x.Amount * x.UnitPrice),
                        OtherMoney = request.OtherMoney, Remark = (request.Remark ?? string.Empty).Trim(), State = 1
                    };
                    db.Insertable(header).ExecuteCommand();
                    foreach (var line in request.Lines)
                    {
                        var barcode = (line.BarCode ?? string.Empty).Trim();
                        if (barcode.Length > 0 && line.Amount != 1) throw new InvalidOperationException("有条码产品每条明细的数量必须为 1。");
                        if (!db.Queryable<Psdr.Models.Entities.Product>().Any(x => x.ProductID == line.ProductID)) throw new InvalidOperationException("入库产品不存在。");
                        if (barcode.Length > 0 && db.Queryable<Psdr.Models.Entities.Stock>().Any(x => x.BarCode == barcode))
                            throw new InvalidOperationException("该条码已在库存中，不能重复入库。");
                        db.Insertable(new StorehouseRecordInfo { No = number, ProductID = line.ProductID, StoreHouseID = request.StoreHouseID, Amount = line.Amount, ThisPrice = line.UnitPrice }).ExecuteCommand();
                        var stock = db.Queryable<Psdr.Models.Entities.Stock>().First(x => x.StoreHouseID == request.StoreHouseID && x.ProductID == line.ProductID && x.BarCode == barcode);
                        var after = line.Amount;
                        if (stock == null) db.Insertable(new Psdr.Models.Entities.Stock { StoreHouseID = request.StoreHouseID, ProductID = line.ProductID, BarCode = barcode, Amount = after }).ExecuteCommand();
                        else { after += stock.Amount ?? 0; stock.Amount = after; db.Updateable(stock).UpdateColumns(x => x.Amount).ExecuteCommand(); }
                        db.Insertable(new StockLedger { BusinessNo = number, BusinessType = "采购入库", StoreHouseID = request.StoreHouseID, ProductID = line.ProductID, BarCode = barcode, ChangeAmount = line.Amount, BalanceAfter = after, OccurredAt = DateTime.Now, Remark = header.Remark }).ExecuteCommand();
                    }
                    // 入库确认货物与供应商应付同时成立；其他费用独立保存，避免重复计入货款。
                    db.Insertable(new FinanceSupply
                    {
                        SupplyID = request.SupplyID,
                        Money = header.TotalMoney,
                        Time = DateTime.Now,
                        Type = 0,
                        No = number,
                        Postage = request.OtherMoney,
                        State = 1,
                        Remark = header.Remark
                    }).ExecuteCommand();
                    WriteOperationLog(db, "采购入库", "入库单号：" + number);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return number;
        }

        /// <summary>创建销售单并从指定仓库扣减可用库存。</summary>
        public string Issue(int storeHouseId, int shopId, string productType, List<StockMoveLine> lines, decimal otherMoney, string remark)
        {
            return Move(false, storeHouseId, shopId, productType, lines, otherMoney, remark);
        }

        /// <summary>
        /// 保存一张待审核销售订单；草稿只记录客户需求，不预扣库存、不产生财务流水。
        /// </summary>
        public string CreateIssueDraft(int storeHouseId, int shopId, string productType, List<StockMoveLine> lines,
            decimal otherMoney, string remark)
        {
            if (storeHouseId <= 0 || shopId <= 0 || lines == null || lines.Count == 0 ||
                lines.Any(x => x.ProductID <= 0 || x.Amount <= 0 || x.UnitPrice < 0))
                throw new InvalidOperationException("仓库、客户和订单明细必须有效。");

            var number = "DD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    if (!db.Queryable<Psdr.Models.Entities.StoreHouse>().Any(x => x.StoreHouseID == storeHouseId) ||
                        !db.Queryable<Psdr.Models.Entities.Shop>().Any(x => x.ShopID == shopId))
                        throw new InvalidOperationException("仓库或客户不存在。");
                    if (lines.Where(x => !string.IsNullOrWhiteSpace(x.BarCode)).Select(x => x.BarCode.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count() != lines.Count(x => !string.IsNullOrWhiteSpace(x.BarCode)))
                        throw new InvalidOperationException("同一订单不能重复录入条码。");

                    db.Insertable(new Indent
                    {
                        No = number, StoreHouseID = storeHouseId, ShopID = shopId, ProductName = productType,
                        Time = DateTime.Now, Amount = lines.Sum(x => x.Amount),
                        TotalMoney = lines.Sum(x => x.Amount * x.UnitPrice), OtherMoney = otherMoney,
                        Remark = (remark ?? string.Empty).Trim(), State = 0
                    }).ExecuteCommand();
                    long sequence = 1;
                    foreach (var line in lines)
                    {
                        var barcode = (line.BarCode ?? string.Empty).Trim();
                        if (barcode.Length > 0 && line.Amount != 1)
                            throw new InvalidOperationException("有条码产品每条明细的数量必须为 1。");
                        if (!db.Queryable<Psdr.Models.Entities.Product>().Any(x => x.ProductID == line.ProductID))
                            throw new InvalidOperationException("订单产品不存在。");
                        db.Insertable(new IndentInfo
                        {
                            No = number, PC = sequence++, ProductID = line.ProductID, Amount = line.Amount,
                            ThisPrice = line.UnitPrice, BarCode = barcode, State = 0, UpdateTime = DateTime.Now
                        }).ExecuteCommand();
                    }
                    WriteOperationLog(db, "销售订单草稿", "订单号：" + number);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return number;
        }

        /// <summary>
        /// 审核一张待审销售订单；审核时才校验库存、扣减余额、登记应收和已售条码。
        /// </summary>
        public void ApproveIssueDraft(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("订单号不能为空。", nameof(number));
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    var header = db.Queryable<Indent>().First(x => x.No == number);
                    if (header == null) throw new InvalidOperationException("待审核订单不存在。");
                    if (header.State != 0) throw new InvalidOperationException("只有待审核订单可以审核。");
                    if (!header.StoreHouseID.HasValue || !header.ShopID.HasValue)
                        throw new InvalidOperationException("订单缺少仓库或客户，不能审核。");
                    var details = db.Queryable<IndentInfo>().Where(x => x.No == number).ToList();
                    if (details.Count == 0) throw new InvalidOperationException("订单没有明细，不能审核。");

                    foreach (var detail in details)
                    {
                        var productId = detail.ProductID ?? 0;
                        var amount = detail.Amount ?? 0;
                        var barcode = (detail.BarCode ?? string.Empty).Trim();
                        if (productId <= 0 || amount <= 0 || (barcode.Length > 0 && amount != 1))
                            throw new InvalidOperationException("订单明细不合法，不能审核。");
                        if (barcode.Length > 0 && db.Queryable<RedPacketList>().Any(x => x.BarCode == barcode))
                            throw new InvalidOperationException("条码 " + barcode + " 已售出，不能重复发货。");
                        var stock = db.Queryable<Psdr.Models.Entities.Stock>().First(x =>
                            x.StoreHouseID == header.StoreHouseID && x.ProductID == productId && x.BarCode == barcode);
                        var before = stock?.Amount ?? 0;
                        if (before < amount) throw new InvalidOperationException("库存不足，无法审核订单。");
                        var after = before - amount;
                        stock.Amount = after;
                        db.Updateable(stock).UpdateColumns(x => x.Amount).ExecuteCommand();
                        detail.State = 1;
                        detail.UpdateTime = DateTime.Now;
                        db.Updateable(detail).UpdateColumns(x => new { x.State, x.UpdateTime }).ExecuteCommand();
                        if (barcode.Length > 0)
                            db.Insertable(new RedPacketList { ProductID = productId, ShopID = header.ShopID.Value, BarCode = barcode, UpdateTime = DateTime.Now }).ExecuteCommand();
                        db.Insertable(new StockLedger
                        {
                            BusinessNo = header.No, BusinessType = "销售出库", StoreHouseID = header.StoreHouseID.Value,
                            ProductID = productId, BarCode = barcode, ChangeAmount = -amount, BalanceAfter = after,
                            OccurredAt = DateTime.Now, Remark = header.Remark
                        }).ExecuteCommand();
                    }
                    header.State = 1;
                    db.Updateable(header).UpdateColumns(x => x.State).ExecuteCommand();
                    db.Insertable(new FinanceShop
                    {
                        ShopID = header.ShopID, Money = header.TotalMoney ?? 0, Time = DateTime.Now, Type = 0,
                        No = header.No, Postage = header.OtherMoney, State = 1, Remark = header.Remark
                    }).ExecuteCommand();
                    WriteOperationLog(db, "销售订单审核", "订单号：" + header.No);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
        }

        /// <summary>
        /// 作废待审核销售订单；因草稿尚未出库，作废不会修改库存或客户账务。
        /// </summary>
        public void VoidIssueDraft(string number)
        {
            if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("订单号不能为空。", nameof(number));
            using (var db = database.Open())
            {
                var transaction = db.Ado.UseTran(() =>
                {
                    var header = db.Queryable<Indent>().First(x => x.No == number);
                    if (header == null) throw new InvalidOperationException("待审核订单不存在。");
                    if (header.State != 0) throw new InvalidOperationException("只有待审核订单可以作废。");
                    header.State = 2;
                    db.Updateable(header).UpdateColumns(x => x.State).ExecuteCommand();
                    db.Updateable<IndentInfo>().SetColumns(x => x.State == 2).Where(x => x.No == number).ExecuteCommand();
                    WriteOperationLog(db, "销售订单作废", "订单号：" + number);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
        }

        /// <summary>创建客户退货单并将产品数量回补到指定仓库。</summary>
        public string Return(int storeHouseId, int shopId, string productType, List<StockMoveLine> lines, decimal otherMoney, string remark)
        {
            return Move(true, storeHouseId, shopId, productType, lines, otherMoney, remark);
        }

        /// <summary>将指定仓库的一件无条码库存绑定为独立条码库存，保持总库存不变。</summary>
        public void BindBarCode(int storeHouseId, int productId, string barCode)
        {
            barCode = (barCode ?? string.Empty).Trim();
            if (storeHouseId <= 0 || productId <= 0 || string.IsNullOrWhiteSpace(barCode)) throw new InvalidOperationException("仓库、产品和条码不能为空。");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    if (db.Queryable<Psdr.Models.Entities.Stock>().Any(x => x.BarCode == barCode) ||
                        db.Queryable<RedPacketList>().Any(x => x.BarCode == barCode))
                        throw new InvalidOperationException("该条码已绑定或已售出，不能重复绑定。");
                    var loose = db.Queryable<Psdr.Models.Entities.Stock>().First(x => x.StoreHouseID == storeHouseId && x.ProductID == productId && (x.BarCode == "" || x.BarCode == null) && x.Amount > 0);
                    if (loose == null) throw new InvalidOperationException("该仓库没有可绑定条码的无条码库存。");
                    loose.Amount -= 1;
                    db.Updateable(loose).UpdateColumns(x => x.Amount).ExecuteCommand();
                    db.Insertable(new Psdr.Models.Entities.Stock { StoreHouseID = storeHouseId, ProductID = productId, BarCode = barCode, Amount = 1 }).ExecuteCommand();
                    var time = DateTime.Now;
                    db.Insertable(new StockLedger { BusinessNo = "BM" + time.ToString("yyyyMMddHHmmssfff"), BusinessType = "条码绑定", StoreHouseID = storeHouseId, ProductID = productId, BarCode = string.Empty, ChangeAmount = -1, BalanceAfter = loose.Amount ?? 0, OccurredAt = time, Remark = "绑定条码：" + barCode }).ExecuteCommand();
                    db.Insertable(new StockLedger { BusinessNo = "BM" + time.ToString("yyyyMMddHHmmssfff"), BusinessType = "条码绑定", StoreHouseID = storeHouseId, ProductID = productId, BarCode = barCode, ChangeAmount = 1, BalanceAfter = 1, OccurredAt = time, Remark = "由无条码库存绑定" }).ExecuteCommand();
                    WriteOperationLog(db, "条码绑定", "条码：" + barCode);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
        }

        /// <summary>解除未售出条码的库存绑定，并将该件库存归还至无条码余额。</summary>
        public void UnbindBarCode(string barCode)
        {
            barCode = (barCode ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(barCode)) throw new InvalidOperationException("请输入要解绑的条码。");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    if (db.Queryable<RedPacketList>().Any(x => x.BarCode == barCode)) throw new InvalidOperationException("该条码已售出，不能解绑。");
                    var coded = db.Queryable<Psdr.Models.Entities.Stock>().First(x => x.BarCode == barCode && x.Amount > 0);
                    if (coded == null) throw new InvalidOperationException("未找到可解绑的在库条码。");
                    var loose = db.Queryable<Psdr.Models.Entities.Stock>().First(x => x.StoreHouseID == coded.StoreHouseID && x.ProductID == coded.ProductID && (x.BarCode == "" || x.BarCode == null));
                    var after = (loose?.Amount ?? 0) + 1;
                    if (loose == null) db.Insertable(new Psdr.Models.Entities.Stock { StoreHouseID = coded.StoreHouseID, ProductID = coded.ProductID, BarCode = string.Empty, Amount = after }).ExecuteCommand();
                    else { loose.Amount = after; db.Updateable(loose).UpdateColumns(x => x.Amount).ExecuteCommand(); }
                    db.Deleteable<Psdr.Models.Entities.Stock>().Where(x => x.StockID == coded.StockID).ExecuteCommand();
                    var time = DateTime.Now; var number = "JB" + time.ToString("yyyyMMddHHmmssfff");
                    db.Insertable(new StockLedger { BusinessNo = number, BusinessType = "条码解绑", StoreHouseID = coded.StoreHouseID ?? 0, ProductID = coded.ProductID ?? 0, BarCode = barCode, ChangeAmount = -1, BalanceAfter = 0, OccurredAt = time, Remark = "解除条码绑定" }).ExecuteCommand();
                    db.Insertable(new StockLedger { BusinessNo = number, BusinessType = "条码解绑", StoreHouseID = coded.StoreHouseID ?? 0, ProductID = coded.ProductID ?? 0, BarCode = string.Empty, ChangeAmount = 1, BalanceAfter = after, OccurredAt = time, Remark = "由条码 " + barCode + " 解绑" }).ExecuteCommand();
                    WriteOperationLog(db, "条码解绑", "条码：" + barCode);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
        }

        /// <summary>
        /// 将盘点实物数量写入库存余额，并登记一笔可追溯的盘点调整流水。
        /// </summary>
        public string AdjustInventory(InventoryAdjustmentRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.StoreHouseID <= 0 || request.ProductID <= 0 || request.ActualAmount < 0)
                throw new InvalidOperationException("仓库、产品和盘点数量必须有效。");
            if (string.IsNullOrWhiteSpace(request.Remark))
                throw new InvalidOperationException("请填写盘点调整原因。");

            var number = "PD" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var barcode = (request.BarCode ?? string.Empty).Trim();
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    var stock = db.Queryable<Psdr.Models.Entities.Stock>().First(x =>
                        x.StoreHouseID == request.StoreHouseID && x.ProductID == request.ProductID &&
                        x.BarCode == barcode);
                    var before = stock?.Amount ?? 0;
                    var change = request.ActualAmount - before;
                    if (change == 0) throw new InvalidOperationException("盘点数量与当前库存一致，无需调整。");

                    if (stock == null)
                    {
                        db.Insertable(new Psdr.Models.Entities.Stock
                        {
                            StoreHouseID = request.StoreHouseID,
                            ProductID = request.ProductID,
                            BarCode = barcode,
                            Amount = request.ActualAmount
                        }).ExecuteCommand();
                    }
                    else
                    {
                        stock.Amount = request.ActualAmount;
                        db.Updateable(stock).UpdateColumns(x => x.Amount).ExecuteCommand();
                    }

                    db.Insertable(new StockLedger
                    {
                        BusinessNo = number,
                        BusinessType = "盘点调整",
                        StoreHouseID = request.StoreHouseID,
                        ProductID = request.ProductID,
                        BarCode = barcode,
                        ChangeAmount = change,
                        BalanceAfter = request.ActualAmount,
                        OccurredAt = DateTime.Now,
                        Remark = request.Remark.Trim()
                    }).ExecuteCommand();
                    WriteOperationLog(db, "盘点调整", "盘点单号：" + number);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return number;
        }

        /// <summary>
        /// 根据扫码盘点结果删除指定仓库、产品中未扫描到的有码库存，并写入盘点流水。
        /// </summary>
        public string ConfirmBarcodeInventory(int storeHouseId, int productId, IEnumerable<string> scannedCodes,
            string remark)
        {
            if (storeHouseId <= 0 || productId <= 0) throw new InvalidOperationException("请选择仓库和产品。");
            if (string.IsNullOrWhiteSpace(remark)) throw new InvalidOperationException("请填写盘点原因。");
            var scanned = new HashSet<string>((scannedCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()), StringComparer.OrdinalIgnoreCase);
            var number = "PM" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    var stocks = db.Queryable<Psdr.Models.Entities.Stock>().Where(x =>
                        x.StoreHouseID == storeHouseId && x.ProductID == productId && x.Amount > 0 &&
                        x.BarCode != null && x.BarCode != string.Empty).ToList();
                    var missing = stocks.Where(x => !scanned.Contains(x.BarCode)).ToList();
                    foreach (var item in missing)
                    {
                        var amount = item.Amount ?? 0;
                        db.Deleteable<Psdr.Models.Entities.Stock>().Where(x => x.StockID == item.StockID).ExecuteCommand();
                        db.Insertable(new StockLedger
                        {
                            BusinessNo = number,
                            BusinessType = "扫码盘点",
                            StoreHouseID = storeHouseId,
                            ProductID = productId,
                            BarCode = item.BarCode,
                            ChangeAmount = -amount,
                            BalanceAfter = 0,
                            OccurredAt = DateTime.Now,
                            Remark = remark.Trim()
                        }).ExecuteCommand();
                    }
                    WriteOperationLog(db, "扫码盘点", "盘点单号：" + number + "，未扫描条码：" + missing.Count + " 个");
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return number;
        }

        private string Move(bool isReturn, int storeHouseId, int shopId, string productType, List<StockMoveLine> lines, decimal otherMoney, string remark)
        {
            if (storeHouseId <= 0 || shopId <= 0 || lines == null || lines.Count == 0 || lines.Any(x => x.ProductID <= 0 || x.Amount <= 0 || x.UnitPrice < 0))
                throw new InvalidOperationException("仓库、客户和单据明细必须有效。");
            var number = (isReturn ? "TH" : "XS") + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            using (var db = database.Open())
            {
                EnsureInventorySchema(db);
                var transaction = db.Ado.UseTran(() =>
                {
                    if (!db.Queryable<Psdr.Models.Entities.StoreHouse>().Any(x => x.StoreHouseID == storeHouseId) || !db.Queryable<Psdr.Models.Entities.Shop>().Any(x => x.ShopID == shopId))
                        throw new InvalidOperationException("仓库或客户不存在。");
                    var total = lines.Sum(x => x.Amount * x.UnitPrice);
                    if (isReturn) db.Insertable(new Psdr.Models.Entities.Return { No = number, StoreHouseID = storeHouseId, ShopID = shopId, ProductName = productType, Time = DateTime.Now, Amount = lines.Sum(x => x.Amount), TotalMoney = total, OtherMoney = otherMoney, Remark = remark, State = 1 }).ExecuteCommand();
                    else db.Insertable(new Indent { No = number, StoreHouseID = storeHouseId, ShopID = shopId, ProductName = productType, Time = DateTime.Now, Amount = lines.Sum(x => x.Amount), TotalMoney = total, OtherMoney = otherMoney, Remark = remark, State = 1 }).ExecuteCommand();
                    foreach (var line in lines)
                    {
                        var barcode = (line.BarCode ?? string.Empty).Trim();
                        if (barcode.Length > 0 && line.Amount != 1) throw new InvalidOperationException("有条码产品每条明细的数量必须为 1。");
                        RedPacketList soldBarcode = null;
                        if (barcode.Length > 0 && isReturn)
                        {
                            soldBarcode = db.Queryable<RedPacketList>().First(x => x.BarCode == barcode && x.ShopID == shopId);
                            if (soldBarcode == null)
                                throw new InvalidOperationException("该条码未销售给当前客户，不能退货。");
                            if (db.Queryable<RedPacketRequestList>().Any(x => x.BarCode == barcode))
                                throw new InvalidOperationException("该条码已兑换红包，不能退货。");
                        }
                        if (barcode.Length > 0 && !isReturn && db.Queryable<RedPacketList>().Any(x => x.BarCode == barcode))
                            throw new InvalidOperationException("该条码已登记为售出，不能重复发货。");
                        var stock = db.Queryable<Psdr.Models.Entities.Stock>().First(x => x.StoreHouseID == storeHouseId && x.ProductID == line.ProductID && x.BarCode == barcode);
                        var before = stock?.Amount ?? 0;
                        if (!isReturn && before < line.Amount) throw new InvalidOperationException("库存不足，无法出库。");
                        var after = isReturn ? before + line.Amount : before - line.Amount;
                        if (stock == null) db.Insertable(new Psdr.Models.Entities.Stock { StoreHouseID = storeHouseId, ProductID = line.ProductID, BarCode = barcode, Amount = after }).ExecuteCommand();
                        else { stock.Amount = after; db.Updateable(stock).UpdateColumns(x => x.Amount).ExecuteCommand(); }
                        if (isReturn) db.Insertable(new ReturnInfo { No = number, ProductID = line.ProductID, Amount = line.Amount, ThisPrice = line.UnitPrice, BarCode = barcode, State = 1, UpdateTime = DateTime.Now }).ExecuteCommand();
                        else db.Insertable(new IndentInfo { No = number, ProductID = line.ProductID, Amount = line.Amount, ThisPrice = line.UnitPrice, BarCode = barcode, State = 1, UpdateTime = DateTime.Now }).ExecuteCommand();
                        if (barcode.Length > 0 && isReturn)
                            db.Deleteable<RedPacketList>().Where(x => x.RedPacketID == soldBarcode.RedPacketID).ExecuteCommand();
                        else if (barcode.Length > 0)
                            db.Insertable(new RedPacketList
                            {
                                ProductID = line.ProductID,
                                ShopID = shopId,
                                BarCode = barcode,
                                UpdateTime = DateTime.Now
                            }).ExecuteCommand();
                        db.Insertable(new StockLedger { BusinessNo = number, BusinessType = isReturn ? "客户退货" : "销售出库", StoreHouseID = storeHouseId, ProductID = line.ProductID, BarCode = barcode, ChangeAmount = isReturn ? line.Amount : -line.Amount, BalanceAfter = after, OccurredAt = DateTime.Now, Remark = remark }).ExecuteCommand();
                    }

                    // 销售产生客户应收，退货产生对应销账；费用单独保存，避免把运费重复计入货款。
                    db.Insertable(new FinanceShop
                    {
                        ShopID = shopId,
                        Money = total,
                        Time = DateTime.Now,
                        Type = isReturn ? 1 : 0,
                        No = number,
                        Postage = otherMoney,
                        State = 1,
                        Remark = remark
                    }).ExecuteCommand();
                    WriteOperationLog(db, isReturn ? "客户退货" : "销售出库", "业务单号：" + number);
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return number;
        }

        /// <summary>确认当前代理库由新版创建脚本生成；只校验，不执行任何自动升级。</summary>
        private static void EnsureInventorySchema(SqlSugar.SqlSugarClient db)
        {
            var ready = db.Ado.SqlQuery<int>(@"
                SELECT CASE WHEN OBJECT_ID(N'dbo.StockLedger', N'U') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Stock', N'StoreHouseID') IS NOT NULL
                    AND COL_LENGTH(N'dbo.StorehouseRecord', N'StoreHouseID') IS NOT NULL
                    THEN 1 ELSE 0 END").FirstOrDefault() == 1;
            if (!ready)
                throw new InvalidOperationException("当前代理库尚未执行库存结构迁移，不能使用新版库存业务。请联系管理员执行交付迁移脚本；客户端不会自动修改代理库结构。");
        }

        /// <summary>
        /// 将关键库存操作写入代理库日志，日志与库存写入使用同一事务。
        /// </summary>
        private static void WriteOperationLog(SqlSugar.SqlSugarClient db, string operation, string information)
        {
            db.Insertable(new Log
            {
                UserName = "系统",
                MachineInfo = Environment.MachineName,
                Event = operation,
                Info = information,
                LogTime = DateTime.Now,
                State = 1
            }).ExecuteCommand();
        }
    }
}
