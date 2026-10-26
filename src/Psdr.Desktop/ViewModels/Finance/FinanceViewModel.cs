using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Finance;
using Psdr.Dals.Shop;
using Psdr.Dals.Supply;
using Psdr.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Finance
{
    /// <summary>管理客户和供应商往来账务的录入与查询。</summary>
    public sealed class FinanceViewModel : BindableBase
    {
        private readonly FinanceDAL dal; private readonly ShopDAL shopDal; private readonly SupplyDAL supplyDal; private readonly IConfirmationService confirmation;
        private string partnerType = "客户"; private Psdr.Models.Entities.Shop selectedShop; private Psdr.Models.Entities.Supply selectedSupply;
        private int transactionType; private decimal money; private string number; private decimal? postage; private string remark; private FinanceListItem selectedItem; private string statusMessage = "正在读取财务流水…"; private bool isBusy;
        /// <summary>初始化财务页面。</summary>
        public FinanceViewModel(FinanceDAL dal, ShopDAL shopDal, SupplyDAL supplyDal, IConfirmationService confirmation)
        {
            this.dal = dal; this.shopDal = shopDal; this.supplyDal = supplyDal; this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => !IsBusy && SelectedItem != null && !SelectedItem.IsBusinessDocument);
        }
        public ObservableCollection<FinanceListItem> Items { get; } = new ObservableCollection<FinanceListItem>();
        public ObservableCollection<Psdr.Models.Entities.Shop> Shops { get; } = new ObservableCollection<Psdr.Models.Entities.Shop>();
        public ObservableCollection<Psdr.Models.Entities.Supply> Supplies { get; } = new ObservableCollection<Psdr.Models.Entities.Supply>();
        public string PartnerType { get => partnerType; set => SetProperty(ref partnerType, value); }
        public Psdr.Models.Entities.Shop SelectedShop { get => selectedShop; set => SetProperty(ref selectedShop, value); }
        public Psdr.Models.Entities.Supply SelectedSupply { get => selectedSupply; set => SetProperty(ref selectedSupply, value); }
        public int TransactionType { get => transactionType; set => SetProperty(ref transactionType, value); }
        public decimal Money { get => money; set => SetProperty(ref money, value); }
        public string Number { get => number; set => SetProperty(ref number, value); }
        public decimal? Postage { get => postage; set => SetProperty(ref postage, value); }
        public string Remark { get => remark; set => SetProperty(ref remark, value); }
        public FinanceListItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!SetProperty(ref selectedItem, value)) return;
                DeleteCommand.RaiseCanExecuteChanged();
                if (value != null && value.IsBusinessDocument)
                    StatusMessage = "该流水由业务单据自动生成，请通过对应采购、销售或退货单据处理。";
            }
        }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; } public DelegateCommand SaveCommand { get; } public DelegateCommand DeleteCommand { get; }
        /// <summary>加载往来单位和流水。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (IsBusy) return; IsBusy = true;
            try { var data = await Task.Run(() => new { Finance = dal.GetAll(), Shops = shopDal.GetAll(), Supplies = supplyDal.GetAll() }); Replace(Items, data.Finance); Replace(Shops, data.Shops); Replace(Supplies, data.Supplies); StatusMessage = $"共 {Items.Count} 条财务流水。"; }
            catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private async Task SaveAsync()
        {
            if (Money <= 0) { StatusMessage = "金额必须大于零。"; return; }
            if (Postage < 0) { StatusMessage = "邮费不能小于零。"; return; }
            IsBusy = true;
            try { if (PartnerType == "客户") { if (SelectedShop == null) throw new InvalidOperationException("请选择客户。"); await Task.Run(() => dal.SaveShop(SelectedShop.ShopID, Money, TransactionType, Number, Postage, Remark)); } else { if (SelectedSupply == null) throw new InvalidOperationException("请选择供应商。"); await Task.Run(() => dal.SaveSupply(SelectedSupply.SupplyID, Money, TransactionType, Number, Postage, Remark)); } Money = 0; Number = Remark = string.Empty; Postage = null; Replace(Items, await Task.Run(() => dal.GetAll())); StatusMessage = "财务流水已保存。"; }
            catch (Exception ex) { StatusMessage = "保存失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private async Task DeleteAsync()
        {
            var item = SelectedItem;
            if (item == null || !confirmation.Confirm($"确认删除{item.PartnerType}“{item.PartnerName}”的财务流水？")) return;
            IsBusy = true;
            try { await Task.Run(() => dal.Delete(item.PartnerType, item.FinanceID)); SelectedItem = null; Replace(Items, await Task.Run(() => dal.GetAll())); StatusMessage = "财务流水已删除。"; }
            catch (Exception ex) { StatusMessage = "删除失败：" + ex.Message; } finally { IsBusy = false; }
        }
        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source) { target.Clear(); foreach (var item in source) target.Add(item); }
    }
}
