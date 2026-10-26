using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Product;
using Psdr.Dals.Stock;
using Psdr.Dals.StoreHouse;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>管理库存条码绑定、解绑操作。</summary>
    public sealed class BarCodeBindingViewModel : BindableBase
    {
        private readonly StoreHouseDAL storeHouseDal; private readonly ProductDAL productDal; private readonly StockTransactionService transactionService;
        private Psdr.Models.Entities.StoreHouse selectedStoreHouse; private Psdr.Models.Entities.Product selectedProduct; private string bindCode; private string unbindCode; private string statusMessage = "正在加载仓库和产品…"; private bool isBusy;
        /// <summary>初始化条码绑定页面。</summary>
        public BarCodeBindingViewModel(StoreHouseDAL storeHouseDal, ProductDAL productDal, StockTransactionService transactionService)
        {
            this.storeHouseDal = storeHouseDal; this.productDal = productDal; this.transactionService = transactionService;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            BindCommand = new DelegateCommand(async () => await BindAsync(), () => !IsBusy);
            UnbindCommand = new DelegateCommand(async () => await UnbindAsync(), () => !IsBusy);
        }
        public ObservableCollection<Psdr.Models.Entities.StoreHouse> StoreHouses { get; } = new ObservableCollection<Psdr.Models.Entities.StoreHouse>();
        public ObservableCollection<Psdr.Models.Entities.Product> Products { get; } = new ObservableCollection<Psdr.Models.Entities.Product>();
        public Psdr.Models.Entities.StoreHouse SelectedStoreHouse { get => selectedStoreHouse; set => SetProperty(ref selectedStoreHouse, value); }
        public Psdr.Models.Entities.Product SelectedProduct { get => selectedProduct; set => SetProperty(ref selectedProduct, value); }
        public string BindCode { get => bindCode; set => SetProperty(ref bindCode, value); }
        public string UnbindCode { get => unbindCode; set => SetProperty(ref unbindCode, value); }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); BindCommand.RaiseCanExecuteChanged(); UnbindCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; } public DelegateCommand BindCommand { get; } public DelegateCommand UnbindCommand { get; }
        /// <summary>加载可用仓库和产品。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (IsBusy) return; IsBusy = true;
            try { var result = await Task.Run(() => new { Stores = storeHouseDal.GetAll(), Products = productDal.GetAll() }); Replace(StoreHouses, result.Stores); Replace(Products, result.Products); SelectedStoreHouse = StoreHouses.FirstOrDefault(); SelectedProduct = Products.FirstOrDefault(); StatusMessage = "请输入或扫描条码后执行绑定、解绑。"; }
            catch (Exception ex) { StatusMessage = "读取资料失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private async Task BindAsync()
        {
            if (SelectedStoreHouse == null || SelectedProduct == null) { StatusMessage = "请选择仓库和产品。"; return; }
            IsBusy = true; try { await Task.Run(() => transactionService.BindBarCode(SelectedStoreHouse.StoreHouseID, SelectedProduct.ProductID, BindCode)); StatusMessage = "条码绑定成功。"; BindCode = string.Empty; } catch (Exception ex) { StatusMessage = "绑定失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private async Task UnbindAsync()
        {
            IsBusy = true; try { await Task.Run(() => transactionService.UnbindBarCode(UnbindCode)); StatusMessage = "条码解绑成功。"; UnbindCode = string.Empty; } catch (Exception ex) { StatusMessage = "解绑失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source) { target.Clear(); foreach (var item in source) target.Add(item); }
    }
}
