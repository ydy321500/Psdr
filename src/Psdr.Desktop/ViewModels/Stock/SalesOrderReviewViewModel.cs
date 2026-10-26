using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Stock;
using Psdr.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>审核待审销售订单，并在审核通过时执行实际出库。</summary>
    public sealed class SalesOrderReviewViewModel : BindableBase
    {
        private readonly SalesOrderReviewDAL dal;
        private readonly StockTransactionService transactionService;
        private readonly IConfirmationService confirmation;
        private SalesOrderReviewItem selectedItem;
        private string statusMessage = "正在读取待审核订单…";
        private bool isBusy;

        /// <summary>初始化订单审核页面。</summary>
        public SalesOrderReviewViewModel(SalesOrderReviewDAL dal, StockTransactionService transactionService,
            IConfirmationService confirmation)
        {
            this.dal = dal; this.transactionService = transactionService; this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            ApproveCommand = new DelegateCommand(async () => await ApproveAsync(), () => !IsBusy && SelectedItem != null);
            VoidCommand = new DelegateCommand(async () => await VoidAsync(), () => !IsBusy && SelectedItem != null);
        }
        public ObservableCollection<SalesOrderReviewItem> Items { get; } = new ObservableCollection<SalesOrderReviewItem>();
        public SalesOrderReviewItem SelectedItem { get => selectedItem; set { if (SetProperty(ref selectedItem, value)) { ApproveCommand.RaiseCanExecuteChanged(); VoidCommand.RaiseCanExecuteChanged(); } } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); ApproveCommand.RaiseCanExecuteChanged(); VoidCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ApproveCommand { get; }
        public DelegateCommand VoidCommand { get; }
        /// <summary>加载未审核且未影响库存的销售订单。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (IsBusy) return; IsBusy = true;
            try { var data = await Task.Run(() => dal.GetPending()); Items.Clear(); foreach (var item in data) Items.Add(item); SelectedItem = null; StatusMessage = "共 " + Items.Count + " 张待审核订单。"; }
            catch (Exception ex) { StatusMessage = "读取待审核订单失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
        private async Task ApproveAsync()
        {
            var item = SelectedItem;
            if (item == null || !confirmation.Confirm("确认审核订单“" + item.No + "”？审核后将扣减库存并登记客户应收。")) return;
            var succeeded = false;
            IsBusy = true;
            try { await Task.Run(() => transactionService.ApproveIssueDraft(item.No)); succeeded = true; StatusMessage = "订单已审核并出库：" + item.No; }
            catch (Exception ex) { StatusMessage = "审核失败：" + ex.Message; } finally { IsBusy = false; }
            if (succeeded) await RefreshAsync();
        }
        private async Task VoidAsync()
        {
            var item = SelectedItem;
            if (item == null || !confirmation.Confirm("确认作废订单“" + item.No + "”？草稿未扣库存，作废不会产生库存或财务变化。")) return;
            var succeeded = false;
            IsBusy = true;
            try { await Task.Run(() => transactionService.VoidIssueDraft(item.No)); succeeded = true; StatusMessage = "订单已作废：" + item.No; }
            catch (Exception ex) { StatusMessage = "作废失败：" + ex.Message; } finally { IsBusy = false; }
            if (succeeded) await RefreshAsync();
        }
    }
}
