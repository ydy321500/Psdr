using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Finance;
using Psdr.Dals.Product;
using Psdr.Dals.Shop;
using Psdr.Dals.Supply;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Finance
{
    /// <summary>维护客户售价和供应商进价。</summary>
    public sealed class PersonPriceViewModel : BindableBase
    {
        private readonly PersonPriceDAL dal; private readonly ShopDAL shopDal; private readonly SupplyDAL supplyDal; private readonly ProductDAL productDal;
        private string priceType = "客户"; private Psdr.Models.Entities.Shop selectedShop; private Psdr.Models.Entities.Supply selectedSupply; private Psdr.Models.Entities.Product selectedProduct; private decimal? price; private decimal? redLow; private decimal? redHigh; private string statusMessage = "正在加载价格资料…"; private bool isBusy;
        /// <summary>初始化专属价格页面。</summary>
        public PersonPriceViewModel(PersonPriceDAL dal, ShopDAL shopDal, SupplyDAL supplyDal, ProductDAL productDal) { this.dal = dal; this.shopDal = shopDal; this.supplyDal = supplyDal; this.productDal = productDal; RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy); SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy); }
        public ObservableCollection<Psdr.Models.Entities.Shop> Shops { get; } = new ObservableCollection<Psdr.Models.Entities.Shop>();
        public ObservableCollection<Psdr.Models.Entities.Supply> Supplies { get; } = new ObservableCollection<Psdr.Models.Entities.Supply>();
        public ObservableCollection<Psdr.Models.Entities.Product> Products { get; } = new ObservableCollection<Psdr.Models.Entities.Product>();
        public string PriceType { get => priceType; set { if (SetProperty(ref priceType, value)) _ = LoadCurrentPriceAsync(); } }
        public Psdr.Models.Entities.Shop SelectedShop { get => selectedShop; set { if (SetProperty(ref selectedShop, value)) _ = LoadCurrentPriceAsync(); } }
        public Psdr.Models.Entities.Supply SelectedSupply { get => selectedSupply; set { if (SetProperty(ref selectedSupply, value)) _ = LoadCurrentPriceAsync(); } }
        public Psdr.Models.Entities.Product SelectedProduct { get => selectedProduct; set { if (SetProperty(ref selectedProduct, value)) _ = LoadCurrentPriceAsync(); } }
        public decimal? Price { get => price; set => SetProperty(ref price, value); } public decimal? RedLow { get => redLow; set => SetProperty(ref redLow, value); } public decimal? RedHigh { get => redHigh; set => SetProperty(ref redHigh, value); }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); } public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; } public DelegateCommand SaveCommand { get; }
        /// <summary>加载客户、供应商和产品。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync() { if (IsBusy) return; IsBusy = true; try { var x = await Task.Run(() => new { Shops = shopDal.GetAll(), Supplies = supplyDal.GetAll(), Products = productDal.GetAll() }); Replace(Shops, x.Shops); Replace(Supplies, x.Supplies); Replace(Products, x.Products); StatusMessage = "选择对象和产品后保存专属价格。"; } catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; } finally { IsBusy = false; } }
        private async Task SaveAsync() { if (SelectedProduct == null || !Price.HasValue || Price < 0 || (PriceType == "客户" && (!RedLow.HasValue || !RedHigh.HasValue || RedLow < 0 || RedHigh < 0 || RedLow > RedHigh))) { StatusMessage = PriceType == "客户" ? "请选择产品，并填写有效的专属售价和红包价格区间。" : "请选择产品，并填写有效的专属进价。"; return; } IsBusy = true; try { if (PriceType == "客户") { if (SelectedShop == null) throw new InvalidOperationException("请选择客户。"); await Task.Run(() => dal.SaveShopPrice(SelectedShop.ShopID, SelectedProduct.ProductID, SelectedProduct.ProductName, Price, RedLow, RedHigh)); } else { if (SelectedSupply == null) throw new InvalidOperationException("请选择供应商。"); await Task.Run(() => dal.SaveSupplyPrice(SelectedSupply.SupplyID, SelectedProduct.ProductID, SelectedProduct.ProductName, Price)); } StatusMessage = "专属价格已保存。"; } catch (Exception ex) { StatusMessage = "保存失败：" + ex.Message; } finally { IsBusy = false; } }
        private async Task LoadCurrentPriceAsync()
        {
            var product = SelectedProduct;
            var shop = SelectedShop;
            var supply = SelectedSupply;
            if (product == null || (PriceType == "客户" && shop == null) || (PriceType != "客户" && supply == null)) return;
            try
            {
                if (PriceType == "客户")
                {
                    var value = await Task.Run(() => dal.GetShopPrices(shop.ShopID).Find(x => x.ProductID == product.ProductID));
                    if (SelectedProduct != product || SelectedShop != shop || PriceType != "客户") return;
                    Price = value?.PersonPricesShop ?? product.Price;
                    RedLow = value?.PersonRedPacketPriceLow ?? product.RedPacketPriceLow;
                    RedHigh = value?.PersonRedPacketPriceHigh ?? product.RedPacketPriceHigh;
                }
                else
                {
                    var value = await Task.Run(() => dal.GetSupplyPrices(supply.SupplyID).Find(x => x.ProductID == product.ProductID));
                    if (SelectedProduct != product || SelectedSupply != supply || PriceType == "客户") return;
                    Price = value?.PersonPricesSupply ?? product.PurchasePrice;
                    RedLow = RedHigh = null;
                }
            }
            catch (Exception ex) { StatusMessage = "读取已保存专属价格失败：" + ex.Message; }
        }
        private static void Replace<T>(ObservableCollection<T> t, System.Collections.Generic.IEnumerable<T> s) { t.Clear(); foreach (var item in s) t.Add(item); }
    }
}
