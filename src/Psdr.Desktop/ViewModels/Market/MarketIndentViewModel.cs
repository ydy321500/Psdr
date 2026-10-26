using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Market;
using Psdr.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Market
{
    /// <summary>
    /// 审核市场端订单的状态，不直接产生桌面端销售单或库存变更。
    /// </summary>
    public sealed class MarketIndentViewModel : BindableBase
    {
        private readonly MarketIndentDAL dal;
        private readonly IConfirmationService confirmation;
        private MarketIndentItem selectedItem;
        private string status = "正在读取市场订单…";
        private bool isBusy;

        /// <summary>
        /// 初始化市场订单审核页。
        /// </summary>
        public MarketIndentViewModel(MarketIndentDAL dal, IConfirmationService confirmation)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            ConfirmCommand = new DelegateCommand(async () => await SetStateAsync(1), CanOperate);
            CloseCommand = new DelegateCommand(async () => await SetStateAsync(2), CanOperate);
        }

        public ObservableCollection<MarketIndentItem> Items { get; } = new ObservableCollection<MarketIndentItem>();
        public MarketIndentItem SelectedItem { get => selectedItem; set { if (SetProperty(ref selectedItem, value)) { ConfirmCommand.RaiseCanExecuteChanged(); CloseCommand.RaiseCanExecuteChanged(); } } }
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); ConfirmCommand.RaiseCanExecuteChanged(); CloseCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CloseCommand { get; }

        /// <summary>
        /// 页面打开时读取市场订单。
        /// </summary>
        public void Load()
        {
            _ = RefreshAsync();
        }

        private bool CanOperate()
        {
            return !IsBusy && SelectedItem != null && SelectedItem.State == 0;
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var records = await Task.Run(() => dal.GetAll());
                Items.Clear();
                foreach (var record in records) Items.Add(record);
                SelectedItem = null;
                Status = $"共 {Items.Count} 条市场订单。";
            }
            catch (Exception ex)
            {
                Status = "读取市场订单失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SetStateAsync(int state)
        {
            if (!CanOperate()) return;
            var operation = state == 1 ? "确认" : "关闭";
            if (!confirmation.Confirm($"确认{operation}市场订单“{SelectedItem.IndentNo}”？该操作不会自动创建销售出库单。")) return;
            IsBusy = true;
            try
            {
                await Task.Run(() => dal.SetState(SelectedItem.IndentID, state));
                Status = "市场订单已" + operation + "。";
            }
            catch (Exception ex)
            {
                Status = operation + "市场订单失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await RefreshAsync();
        }
    }
}
