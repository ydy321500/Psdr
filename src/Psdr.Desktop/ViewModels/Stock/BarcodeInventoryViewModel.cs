using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Product;
using Psdr.Dals.Stock;
using Psdr.Dals.StoreHouse;
using Psdr.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>
    /// 执行有条码库存的扫码盘点，并对未扫描条码生成盘点扣减流水。
    /// </summary>
    public sealed class BarcodeInventoryViewModel : BindableBase
    {
        private readonly StockDAL stockDal;
        private readonly StoreHouseDAL storeHouseDal;
        private readonly ProductDAL productDal;
        private readonly StockTransactionService transactionService;
        private readonly IConfirmationService confirmation;
        private Psdr.Models.Entities.StoreHouse selectedStoreHouse;
        private Psdr.Models.Entities.Product selectedProduct;
        private string scanCode;
        private string remark;
        private string status = "请选择仓库和产品后读取在库条码。";
        private bool isBusy;

        /// <summary>
        /// 初始化扫码盘点页面所需的基础资料、库存服务和操作命令。
        /// </summary>
        public BarcodeInventoryViewModel(StockDAL stockDal, StoreHouseDAL storeHouseDal,
            ProductDAL productDal, StockTransactionService transactionService, IConfirmationService confirmation)
        {
            this.stockDal = stockDal;
            this.storeHouseDal = storeHouseDal;
            this.productDal = productDal;
            this.transactionService = transactionService;
            this.confirmation = confirmation;
            RefreshSourcesCommand = new DelegateCommand(async () => await RefreshSourcesAsync(), () => !IsBusy);
            LoadCodesCommand = new DelegateCommand(async () => await LoadCodesAsync(), CanLoadCodes);
            ScanCommand = new DelegateCommand(Scan, CanScan);
            SubmitCommand = new DelegateCommand(async () => await SubmitAsync(), CanSubmit);
        }

        public ObservableCollection<Psdr.Models.Entities.StoreHouse> StoreHouses { get; } =
            new ObservableCollection<Psdr.Models.Entities.StoreHouse>();
        public ObservableCollection<Psdr.Models.Entities.Product> Products { get; } =
            new ObservableCollection<Psdr.Models.Entities.Product>();
        public ObservableCollection<BarcodeInventoryItem> Items { get; } =
            new ObservableCollection<BarcodeInventoryItem>();

        public Psdr.Models.Entities.StoreHouse SelectedStoreHouse
        {
            get => selectedStoreHouse;
            set { if (SetProperty(ref selectedStoreHouse, value)) LoadCodesCommand.RaiseCanExecuteChanged(); }
        }
        public Psdr.Models.Entities.Product SelectedProduct
        {
            get => selectedProduct;
            set { if (SetProperty(ref selectedProduct, value)) LoadCodesCommand.RaiseCanExecuteChanged(); }
        }
        public string ScanCode
        {
            get => scanCode;
            set { if (SetProperty(ref scanCode, value)) ScanCommand.RaiseCanExecuteChanged(); }
        }
        public string Remark
        {
            get => remark;
            set { if (SetProperty(ref remark, value)) SubmitCommand.RaiseCanExecuteChanged(); }
        }
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public int ScannedCount => Items.Count(x => x.IsScanned);
        public int MissingCount => Items.Count - ScannedCount;
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshSourcesCommand.RaiseCanExecuteChanged();
                    LoadCodesCommand.RaiseCanExecuteChanged();
                    ScanCommand.RaiseCanExecuteChanged();
                    SubmitCommand.RaiseCanExecuteChanged();
                }
            }
        }
        public DelegateCommand RefreshSourcesCommand { get; }
        public DelegateCommand LoadCodesCommand { get; }
        public DelegateCommand ScanCommand { get; }
        public DelegateCommand SubmitCommand { get; }

        /// <summary>
        /// 页面打开时读取仓库和产品资料。
        /// </summary>
        public void Load()
        {
            _ = RefreshSourcesAsync();
        }

        private bool CanLoadCodes()
        {
            return !IsBusy && SelectedStoreHouse != null && SelectedProduct != null;
        }

        private bool CanScan()
        {
            return !IsBusy && Items.Count > 0 && !string.IsNullOrWhiteSpace(ScanCode);
        }

        private bool CanSubmit()
        {
            return !IsBusy && Items.Count > 0 && !string.IsNullOrWhiteSpace(Remark);
        }

        private async Task RefreshSourcesAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var data = await Task.Run(() => new { Stores = storeHouseDal.GetAll(), Products = productDal.GetAll() });
                Replace(StoreHouses, data.Stores);
                Replace(Products, data.Products);
                SelectedStoreHouse = StoreHouses.FirstOrDefault();
                SelectedProduct = Products.FirstOrDefault();
                Status = "请选择后点击“读取在库条码”。";
            }
            catch (Exception ex)
            {
                Status = "读取基础资料失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCodesAsync()
        {
            if (!CanLoadCodes()) return;
            IsBusy = true;
            try
            {
                var codes = await Task.Run(() => stockDal.GetBarcodeInventory(
                    SelectedStoreHouse.StoreHouseID, SelectedProduct.ProductID));
                Replace(Items, codes);
                RaiseCounts();
                Status = $"已读取 {Items.Count} 个在库条码，请逐个扫描实物。";
            }
            catch (Exception ex)
            {
                Status = "读取在库条码失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Scan()
        {
            var code = ScanCode?.Trim();
            var item = Items.FirstOrDefault(x => string.Equals(x.BarCode, code, StringComparison.OrdinalIgnoreCase));
            if (item == null) Status = "该条码不在本次盘点范围内。";
            else if (item.IsScanned) Status = "该条码已扫描。";
            else
            {
                var index = Items.IndexOf(item);
                Items[index] = new BarcodeInventoryItem
                {
                    StockID = item.StockID,
                    BarCode = item.BarCode,
                    Amount = item.Amount,
                    IsScanned = true
                };
                RaiseCounts();
                Status = "扫描成功。";
            }
            ScanCode = string.Empty;
            ScanCommand.RaiseCanExecuteChanged();
        }

        private async Task SubmitAsync()
        {
            if (!CanSubmit()) return;
            if (!confirmation.Confirm($"确认提交盘点？将从库存中移除 {MissingCount} 个未扫描条码，并写入库存流水。")) return;
            IsBusy = true;
            try
            {
                var number = await Task.Run(() => transactionService.ConfirmBarcodeInventory(
                    SelectedStoreHouse.StoreHouseID, SelectedProduct.ProductID,
                    Items.Where(x => x.IsScanned).Select(x => x.BarCode), Remark));
                Status = "扫码盘点已提交，流水号：" + number;
                await LoadCodesAfterSubmitAsync();
            }
            catch (Exception ex)
            {
                Status = "提交盘点失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadCodesAfterSubmitAsync()
        {
            var codes = await Task.Run(() => stockDal.GetBarcodeInventory(
                SelectedStoreHouse.StoreHouseID, SelectedProduct.ProductID));
            Replace(Items, codes);
            RaiseCounts();
        }

        private void RaiseCounts()
        {
            RaisePropertyChanged(nameof(ScannedCount));
            RaisePropertyChanged(nameof(MissingCount));
            SubmitCommand.RaiseCanExecuteChanged();
        }

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
