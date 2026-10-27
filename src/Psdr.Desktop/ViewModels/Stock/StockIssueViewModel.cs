using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Finance;
using Psdr.Dals.Product;
using Psdr.Dals.Shop;
using Psdr.Dals.Stock;
using Psdr.Dals.StoreHouse;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>表示尚未提交的销售出库明细。</summary>
    public sealed class StockIssueLineEditor : BindableBase
    {
        private int amount;
        private decimal unitPrice;
        private string barCode;
        /// <summary>使用产品资料初始化一条销售出库明细。</summary>
        public StockIssueLineEditor(Psdr.Models.Entities.Product product, int amount, decimal unitPrice, string barCode)
        {
            ProductID = product.ProductID; ProductType = product.ProductName;
            ProductDisplay = $"{product.ProductID} - {product.ProductName} {product.K1} {product.K2}".Trim();
            this.amount = amount; this.unitPrice = unitPrice; this.barCode = barCode;
        }
        public int ProductID { get; }
        public string ProductType { get; }
        public string ProductDisplay { get; }
        public int Amount { get => amount; set { if (SetProperty(ref amount, value)) RaisePropertyChanged(nameof(Total)); } }
        public decimal UnitPrice { get => unitPrice; set { if (SetProperty(ref unitPrice, value)) RaisePropertyChanged(nameof(Total)); } }
        public string BarCode { get => barCode; set => SetProperty(ref barCode, value); }
        public decimal Total => Amount * UnitPrice;
    }

    /// <summary>组织销售出库的单头、明细和库存扣减操作。</summary>
    public class StockIssueViewModel : BindableBase
    {
        private readonly StoreHouseDAL storeHouseDal;
        private readonly ShopDAL shopDal;
        private readonly ProductDAL productDal;
        private readonly PersonPriceDAL personPriceDal;
        private readonly StockTransactionService transactionService;
        private Psdr.Models.Entities.StoreHouse selectedStoreHouse;
        private Psdr.Models.Entities.Shop selectedShop;
        private Psdr.Models.Entities.Product selectedProduct;
        private int lineAmount = 1;
        private decimal lineUnitPrice;
        private string lineBarCode;
        private decimal otherMoney;
        private string remark;
        private string statusMessage;
        private bool isBusy;

        /// <summary>初始化销售出库页面使用的数据服务和操作命令。</summary>
        public StockIssueViewModel(StoreHouseDAL storeHouseDal, ShopDAL shopDal, ProductDAL productDal,
            PersonPriceDAL personPriceDal, StockTransactionService transactionService)
        {
            this.storeHouseDal = storeHouseDal; this.shopDal = shopDal; this.productDal = productDal; this.personPriceDal = personPriceDal; this.transactionService = transactionService;
            statusMessage = "正在准备" + PageTitle + "资料…";
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            AddLineCommand = new DelegateCommand(AddLine, () => !IsBusy && SelectedProduct != null && LineAmount > 0 && LineUnitPrice >= 0);
            RemoveLineCommand = new DelegateCommand<StockIssueLineEditor>(RemoveLine, line => !IsBusy && line != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && Lines.Count > 0);
            SaveDraftCommand = new DelegateCommand(async () => await SaveDraftAsync(), () => !IsBusy && Lines.Count > 0 && !IsReturnMode);
        }
        public ObservableCollection<Psdr.Models.Entities.StoreHouse> StoreHouses { get; } = new ObservableCollection<Psdr.Models.Entities.StoreHouse>();
        public ObservableCollection<Psdr.Models.Entities.Shop> Shops { get; } = new ObservableCollection<Psdr.Models.Entities.Shop>();
        public ObservableCollection<Psdr.Models.Entities.Product> Products { get; } = new ObservableCollection<Psdr.Models.Entities.Product>();
        public ObservableCollection<StockIssueLineEditor> Lines { get; } = new ObservableCollection<StockIssueLineEditor>();
        public Psdr.Models.Entities.StoreHouse SelectedStoreHouse { get => selectedStoreHouse; set => SetProperty(ref selectedStoreHouse, value); }
        public Psdr.Models.Entities.Shop SelectedShop { get => selectedShop; set { if (SetProperty(ref selectedShop, value)) _ = LoadPreferredPriceAsync(); } }
        public Psdr.Models.Entities.Product SelectedProduct { get => selectedProduct; set { if (SetProperty(ref selectedProduct, value)) { if (value != null) { LineUnitPrice = value.Price ?? 0; _ = LoadPreferredPriceAsync(); } AddLineCommand.RaiseCanExecuteChanged(); } } }
        public int LineAmount { get => lineAmount; set { if (SetProperty(ref lineAmount, value)) AddLineCommand.RaiseCanExecuteChanged(); } }
        public decimal LineUnitPrice { get => lineUnitPrice; set { if (SetProperty(ref lineUnitPrice, value)) AddLineCommand.RaiseCanExecuteChanged(); } }
        public string LineBarCode { get => lineBarCode; set => SetProperty(ref lineBarCode, value); }
        public decimal OtherMoney { get => otherMoney; set => SetProperty(ref otherMoney, value); }
        public string Remark { get => remark; set => SetProperty(ref remark, value); }
        public decimal TotalMoney => Lines.Sum(x => x.Total) + OtherMoney;
        public virtual string PageTitle => "销售出库";
        public virtual string PageDescription => "保存时生成销售单、从指定仓库扣减库存、登记客户应收并记录库存流水；库存不足时不会保存任何业务数据";
        public virtual string StoreHouseLabel => "出库仓库 *";
        public virtual string CustomerLabel => "客户 *";
        public virtual string LineSectionTitle => "添加出库明细";
        public virtual string LinePriceToolTip => "销售单价";
        public virtual string SaveButtonText => "保存出库";
        /// <summary>提供当前出库或退货模式对应的新增按钮文案。</summary>
        public virtual string NewButtonText => "新增出库单";
        protected virtual bool IsReturnMode => false;
        /// <summary>指示当前页面是否允许将销售明细保存为待审核订单。</summary>
        public virtual bool CanSaveDraft => !IsReturnMode;
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); AddLineCommand.RaiseCanExecuteChanged(); RemoveLineCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged(); SaveDraftCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddLineCommand { get; }
        public DelegateCommand<StockIssueLineEditor> RemoveLineCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand SaveDraftCommand { get; }
        /// <summary>加载仓库、客户和产品下拉数据。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (IsBusy) return; IsBusy = true;
            try
            {
                var result = await Task.Run(() => new { StoreHouses = storeHouseDal.GetAll(), Shops = shopDal.GetAll(), Products = productDal.GetAll() });
                Replace(StoreHouses, result.StoreHouses); Replace(Shops, result.Shops); Replace(Products, result.Products);
                SelectedStoreHouse = StoreHouses.FirstOrDefault(); SelectedShop = Shops.FirstOrDefault(); SelectedProduct = Products.FirstOrDefault();
                StatusMessage = $"已加载 {StoreHouses.Count} 个仓库、{Shops.Count} 个客户和 {Products.Count} 个产品。";
            }
            catch (Exception ex) { StatusMessage = "读取资料失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
        private void AddLine()
        {
            if (SelectedProduct == null || LineAmount <= 0 || LineUnitPrice < 0) return;
            var barcode = (LineBarCode ?? string.Empty).Trim();
            if (barcode.Length > 0 && LineAmount != 1) { StatusMessage = "录入条码时，每条明细数量必须为 1。"; return; }
            var line = new StockIssueLineEditor(SelectedProduct, LineAmount, LineUnitPrice, barcode);
            line.PropertyChanged += OnLinePropertyChanged;
            Lines.Add(line); LineAmount = 1; LineBarCode = string.Empty;
            RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged(); SaveDraftCommand.RaiseCanExecuteChanged();
            StatusMessage = IsReturnMode ? "已加入退货明细，保存时会回补指定仓库库存。" : "已加入出库明细，保存时会校验库存是否充足。";
        }
        /// <summary>
        /// 在销售录单中带入客户专属售价；客户退货保持手工填写的原销售单价。
        /// </summary>
        private async Task LoadPreferredPriceAsync()
        {
            if (IsReturnMode) return;
            var shop = SelectedShop;
            var product = SelectedProduct;
            if (shop == null || product == null) return;
            try
            {
                var price = await Task.Run(() => personPriceDal.GetShopPrices(shop.ShopID)
                    .FirstOrDefault(x => x.ProductID == product.ProductID)?.PersonPricesShop);
                if (SelectedShop != shop || SelectedProduct != product || IsReturnMode || !price.HasValue) return;
                LineUnitPrice = price.Value;
                StatusMessage = "已带入客户专属售价，可按本次业务需要修改。";
            }
            catch (Exception ex) { StatusMessage = "读取客户专属售价失败：" + ex.Message; }
        }
        private void RemoveLine(StockIssueLineEditor line) { if (line == null) return; line.PropertyChanged -= OnLinePropertyChanged; Lines.Remove(line); RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged(); SaveDraftCommand.RaiseCanExecuteChanged(); }
        private async Task SaveAsync()
        {
            if (IsBusy) return;
            if (SelectedStoreHouse == null || SelectedShop == null) { StatusMessage = "请选择仓库和客户。"; return; }
            if (OtherMoney < 0) { StatusMessage = "其他费用不能小于零。"; return; }
            var lines = Lines.ToList(); if (lines.Count == 0) { StatusMessage = "请至少加入一条出库明细。"; return; }
            var productType = lines.Select(x => x.ProductType).Distinct().Count() == 1 ? lines[0].ProductType : "混合产品";
            IsBusy = true;
            try
            {
                var moveLines = lines.Select(x => new StockMoveLine { ProductID = x.ProductID, Amount = x.Amount, UnitPrice = x.UnitPrice, BarCode = x.BarCode }).ToList();
                var number = await Task.Run(() => IsReturnMode
                    ? transactionService.Return(SelectedStoreHouse.StoreHouseID, SelectedShop.ShopID, productType, moveLines, OtherMoney, (Remark ?? string.Empty).Trim())
                    : transactionService.Issue(SelectedStoreHouse.StoreHouseID, SelectedShop.ShopID, productType, moveLines, OtherMoney, (Remark ?? string.Empty).Trim()));
                foreach (var line in Lines) line.PropertyChanged -= OnLinePropertyChanged;
                Lines.Clear(); OtherMoney = 0; Remark = string.Empty; RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged(); SaveDraftCommand.RaiseCanExecuteChanged(); StatusMessage = PageTitle + "已保存，单号：" + number;
            }
            catch (Exception ex) { StatusMessage = "保存出库失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
        /// <summary>
        /// 将当前销售明细保存为待审核订单，不产生库存变动或客户财务流水。
        /// </summary>
        private async Task SaveDraftAsync()
        {
            if (IsReturnMode || IsBusy) return;
            if (SelectedStoreHouse == null || SelectedShop == null) { StatusMessage = "请选择仓库和客户。"; return; }
            if (OtherMoney < 0) { StatusMessage = "其他费用不能小于零。"; return; }
            var lines = Lines.ToList(); if (lines.Count == 0) { StatusMessage = "请至少加入一条订单明细。"; return; }
            var productType = lines.Select(x => x.ProductType).Distinct().Count() == 1 ? lines[0].ProductType : "混合产品";
            IsBusy = true;
            try
            {
                var moveLines = lines.Select(x => new StockMoveLine { ProductID = x.ProductID, Amount = x.Amount, UnitPrice = x.UnitPrice, BarCode = x.BarCode }).ToList();
                var number = await Task.Run(() => transactionService.CreateIssueDraft(SelectedStoreHouse.StoreHouseID,
                    SelectedShop.ShopID, productType, moveLines, OtherMoney, (Remark ?? string.Empty).Trim()));
                foreach (var line in Lines) line.PropertyChanged -= OnLinePropertyChanged;
                Lines.Clear(); OtherMoney = 0; Remark = string.Empty; RaisePropertyChanged(nameof(TotalMoney));
                SaveCommand.RaiseCanExecuteChanged(); SaveDraftCommand.RaiseCanExecuteChanged();
                StatusMessage = "待审核销售订单已保存，订单号：" + number + "。审核前不会占用库存。";
            }
            catch (Exception ex) { StatusMessage = "保存待审核订单失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
        private void OnLinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StockIssueLineEditor.Amount) || e.PropertyName == nameof(StockIssueLineEditor.UnitPrice))
                RaisePropertyChanged(nameof(TotalMoney));
        }
        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source) { target.Clear(); foreach (var item in source) target.Add(item); }
    }

    /// <summary>复用销售出库录单界面，创建客户退货并回补库存。</summary>
    public sealed class StockReturnViewModel : StockIssueViewModel
    {
        /// <summary>初始化客户退货页面。</summary>
        public StockReturnViewModel(StoreHouseDAL storeHouseDal, ShopDAL shopDal, ProductDAL productDal,
            PersonPriceDAL personPriceDal, StockTransactionService transactionService) : base(storeHouseDal, shopDal, productDal, personPriceDal, transactionService) { }

        public override string PageTitle => "客户退货";
        public override string PageDescription => "保存时生成退货单、回补指定仓库库存、登记客户销账并记录库存流水";
        public override string StoreHouseLabel => "回补仓库 *";
        public override string CustomerLabel => "退货客户 *";
        public override string LineSectionTitle => "添加退货明细";
        public override string LinePriceToolTip => "退货单价";
        public override string SaveButtonText => "保存退货";
        /// <summary>为客户退货页面显示新增退货单入口。</summary>
        public override string NewButtonText => "新增退货单";
        protected override bool IsReturnMode => true;
        public override bool CanSaveDraft => false;
    }
}
