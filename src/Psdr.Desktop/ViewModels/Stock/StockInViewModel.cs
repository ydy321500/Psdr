using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Finance;
using Psdr.Dals.Product;
using Psdr.Dals.Stock;
using Psdr.Dals.StoreHouse;
using Psdr.Dals.Supply;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>表示尚未提交的采购入库明细。</summary>
    public sealed class StockInLineEditor : BindableBase
    {
        private int amount;
        private decimal unitPrice;
        private string barCode;

        /// <summary>使用产品资料初始化一条采购入库明细。</summary>
        public StockInLineEditor(Psdr.Models.Entities.Product product, int amount, decimal unitPrice, string barCode)
        {
            ProductID = product.ProductID;
            ProductType = product.ProductName;
            ProductDisplay = $"{product.ProductID} - {product.ProductName} {product.K1} {product.K2}".Trim();
            this.amount = amount;
            this.unitPrice = unitPrice;
            this.barCode = barCode;
        }

        public int ProductID { get; }
        public string ProductType { get; }
        public string ProductDisplay { get; }
        public int Amount { get => amount; set { if (SetProperty(ref amount, value)) RaisePropertyChanged(nameof(Total)); } }
        public decimal UnitPrice { get => unitPrice; set { if (SetProperty(ref unitPrice, value)) RaisePropertyChanged(nameof(Total)); } }
        public string BarCode { get => barCode; set => SetProperty(ref barCode, value); }
        public decimal Total => Amount * UnitPrice;
    }

    /// <summary>组织采购入库的单头、明细和库存写入操作。</summary>
    public sealed class StockInViewModel : BindableBase
    {
        private readonly StoreHouseDAL storeHouseDal;
        private readonly SupplyDAL supplyDal;
        private readonly ProductDAL productDal;
        private readonly PersonPriceDAL personPriceDal;
        private readonly StockTransactionService transactionService;
        private Psdr.Models.Entities.StoreHouse selectedStoreHouse;
        private Psdr.Models.Entities.Supply selectedSupply;
        private Psdr.Models.Entities.Product selectedProduct;
        private int lineAmount = 1;
        private decimal lineUnitPrice;
        private string lineBarCode;
        private decimal otherMoney;
        private string remark;
        private string statusMessage = "正在准备入库资料…";
        private bool isBusy;

        /// <summary>初始化采购入库页面使用的数据服务和操作命令。</summary>
        public StockInViewModel(StoreHouseDAL storeHouseDal, SupplyDAL supplyDal, ProductDAL productDal,
            PersonPriceDAL personPriceDal, StockTransactionService transactionService)
        {
            this.storeHouseDal = storeHouseDal;
            this.supplyDal = supplyDal;
            this.productDal = productDal;
            this.personPriceDal = personPriceDal;
            this.transactionService = transactionService;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            AddLineCommand = new DelegateCommand(AddLine, () => !IsBusy && SelectedProduct != null && LineAmount > 0 && LineUnitPrice >= 0);
            RemoveLineCommand = new DelegateCommand<StockInLineEditor>(RemoveLine, line => !IsBusy && line != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && Lines.Count > 0);
        }

        public ObservableCollection<Psdr.Models.Entities.StoreHouse> StoreHouses { get; } = new ObservableCollection<Psdr.Models.Entities.StoreHouse>();
        public ObservableCollection<Psdr.Models.Entities.Supply> Supplies { get; } = new ObservableCollection<Psdr.Models.Entities.Supply>();
        public ObservableCollection<Psdr.Models.Entities.Product> Products { get; } = new ObservableCollection<Psdr.Models.Entities.Product>();
        public ObservableCollection<StockInLineEditor> Lines { get; } = new ObservableCollection<StockInLineEditor>();
        public Psdr.Models.Entities.StoreHouse SelectedStoreHouse { get => selectedStoreHouse; set => SetProperty(ref selectedStoreHouse, value); }
        public Psdr.Models.Entities.Supply SelectedSupply
        {
            get => selectedSupply;
            set
            {
                if (SetProperty(ref selectedSupply, value)) _ = LoadPreferredPriceAsync();
            }
        }
        public Psdr.Models.Entities.Product SelectedProduct
        {
            get => selectedProduct;
            set
            {
                if (!SetProperty(ref selectedProduct, value)) return;
                if (value != null) { LineUnitPrice = value.PurchasePrice ?? 0; _ = LoadPreferredPriceAsync(); }
                AddLineCommand.RaiseCanExecuteChanged();
            }
        }
        public int LineAmount { get => lineAmount; set { if (SetProperty(ref lineAmount, value)) AddLineCommand.RaiseCanExecuteChanged(); } }
        public decimal LineUnitPrice { get => lineUnitPrice; set { if (SetProperty(ref lineUnitPrice, value)) AddLineCommand.RaiseCanExecuteChanged(); } }
        public string LineBarCode { get => lineBarCode; set => SetProperty(ref lineBarCode, value); }
        public decimal OtherMoney { get => otherMoney; set => SetProperty(ref otherMoney, value); }
        public string Remark { get => remark; set => SetProperty(ref remark, value); }
        public decimal TotalMoney => Lines.Sum(x => x.Total) + OtherMoney;
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged(); AddLineCommand.RaiseCanExecuteChanged();
                RemoveLineCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged();
            }
        }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AddLineCommand { get; }
        public DelegateCommand<StockInLineEditor> RemoveLineCommand { get; }
        public DelegateCommand SaveCommand { get; }

        /// <summary>加载仓库、供应商和产品下拉数据。</summary>
        public void Load() { _ = RefreshAsync(); }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var result = await Task.Run(() => new
                {
                    StoreHouses = storeHouseDal.GetAll(),
                    Supplies = supplyDal.GetAll(),
                    Products = productDal.GetAll()
                });
                Replace(StoreHouses, result.StoreHouses); Replace(Supplies, result.Supplies); Replace(Products, result.Products);
                SelectedStoreHouse = StoreHouses.FirstOrDefault();
                SelectedSupply = Supplies.FirstOrDefault();
                SelectedProduct = Products.FirstOrDefault();
                StatusMessage = $"已加载 {StoreHouses.Count} 个仓库、{Supplies.Count} 个供应商和 {Products.Count} 个产品。";
            }
            catch (Exception ex) { StatusMessage = "读取资料失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private void AddLine()
        {
            var product = SelectedProduct;
            if (product == null || LineAmount <= 0 || LineUnitPrice < 0) return;
            var barcode = (LineBarCode ?? string.Empty).Trim();
            if (barcode.Length > 0 && LineAmount != 1) { StatusMessage = "录入条码时，每条明细数量必须为 1。"; return; }
            var line = new StockInLineEditor(product, LineAmount, LineUnitPrice, barcode);
            line.PropertyChanged += OnLinePropertyChanged;
            Lines.Add(line);
            LineAmount = 1; LineBarCode = string.Empty;
            RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged();
            StatusMessage = "已加入入库明细，确认无误后点击“保存入库”。";
        }

        /// <summary>
        /// 在已选择供应商和产品时，读取并带入供应商专属进价。
        /// </summary>
        private async Task LoadPreferredPriceAsync()
        {
            var supply = SelectedSupply;
            var product = SelectedProduct;
            if (supply == null || product == null) return;
            try
            {
                var price = await Task.Run(() => personPriceDal.GetSupplyPrices(supply.SupplyID)
                    .FirstOrDefault(x => x.ProductID == product.ProductID)?.PersonPricesSupply);
                if (SelectedSupply != supply || SelectedProduct != product || !price.HasValue) return;
                LineUnitPrice = price.Value;
                StatusMessage = "已带入供应商专属进价，可按本次业务需要修改。";
            }
            catch (Exception ex) { StatusMessage = "读取供应商专属进价失败：" + ex.Message; }
        }

        private void RemoveLine(StockInLineEditor line)
        {
            if (line == null) return;
            line.PropertyChanged -= OnLinePropertyChanged;
            Lines.Remove(line); RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            if (IsBusy) return;
            if (SelectedStoreHouse == null || SelectedSupply == null) { StatusMessage = "请选择仓库和供应商。"; return; }
            if (OtherMoney < 0) { StatusMessage = "其他费用不能小于零。"; return; }
            var lines = Lines.ToList();
            if (lines.Count == 0) { StatusMessage = "请至少加入一条入库明细。"; return; }
            var productType = lines.Select(x => x.ProductType).Distinct().Count() == 1 ? lines[0].ProductType : "混合产品";
            var request = new StockInRequest
            {
                StoreHouseID = SelectedStoreHouse.StoreHouseID, SupplyID = SelectedSupply.SupplyID, ProductType = productType,
                OtherMoney = OtherMoney, Remark = (Remark ?? string.Empty).Trim(),
                Lines = lines.Select(x => new StockInLine { ProductID = x.ProductID, Amount = x.Amount, UnitPrice = x.UnitPrice, BarCode = x.BarCode }).ToList()
            };
            IsBusy = true;
            try
            {
                var number = await Task.Run(() => transactionService.Receive(request));
                foreach (var line in Lines) line.PropertyChanged -= OnLinePropertyChanged;
                Lines.Clear(); OtherMoney = 0; Remark = string.Empty; RaisePropertyChanged(nameof(TotalMoney)); SaveCommand.RaiseCanExecuteChanged();
                StatusMessage = "入库已保存，单号：" + number;
            }
            catch (Exception ex) { StatusMessage = "保存入库失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear(); foreach (var item in source) target.Add(item);
        }

        private void OnLinePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StockInLineEditor.Amount) || e.PropertyName == nameof(StockInLineEditor.UnitPrice))
                RaisePropertyChanged(nameof(TotalMoney));
        }
    }
}
