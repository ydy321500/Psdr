using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Stock;
using Psdr.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>
    /// 展示库存余额与流水差异，并在人工确认后登记盘点调整。
    /// </summary>
    public sealed class InventoryCheckViewModel : BindableBase
    {
        private readonly StockDAL dal;
        private readonly StockTransactionService transactionService;
        private readonly IConfirmationService confirmation;
        private string statusMessage = "正在核对库存…";
        private bool isBusy;
        private StockReconciliationItem selectedItem;
        private string actualAmount;
        private string adjustmentRemark;

        /// <summary>
        /// 初始化库存核对、盘点调整所需的数据服务及命令。
        /// </summary>
        public InventoryCheckViewModel(StockDAL dal, StockTransactionService transactionService,
            IConfirmationService confirmation)
        {
            this.dal = dal;
            this.transactionService = transactionService;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            AdjustCommand = new DelegateCommand(async () => await AdjustAsync(), CanAdjust);
        }

        public ObservableCollection<StockReconciliationItem> Items { get; } =
            new ObservableCollection<StockReconciliationItem>();

        public string StatusMessage
        {
            get => statusMessage;
            private set => SetProperty(ref statusMessage, value);
        }

        public StockReconciliationItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (SetProperty(ref selectedItem, value))
                {
                    ActualAmount = value?.StockAmount?.ToString();
                    AdjustCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string ActualAmount
        {
            get => actualAmount;
            set
            {
                if (SetProperty(ref actualAmount, value)) AdjustCommand.RaiseCanExecuteChanged();
            }
        }

        public string AdjustmentRemark
        {
            get => adjustmentRemark;
            set
            {
                if (SetProperty(ref adjustmentRemark, value)) AdjustCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    AdjustCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand AdjustCommand { get; }

        /// <summary>
        /// 页面打开时读取库存余额与最后一笔流水的差异。
        /// </summary>
        public void Load()
        {
            _ = RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var data = await Task.Run(() => new
                {
                    HasLedger = dal.HasStockLedger(),
                    Items = dal.GetReconciliation()
                });
                Items.Clear();
                foreach (var item in data.Items) Items.Add(item);
                SelectedItem = null;
                StatusMessage = data.HasLedger
                    ? (Items.Count == 0
                        ? "库存余额与流水余额一致。"
                        : $"发现 {Items.Count} 条库存差异，请核对业务单据和库存流水后再决定是否盘点调整。 ")
                    : "当前代理库未启用库存流水，无法执行库存核对。";
            }
            catch (Exception ex)
            {
                StatusMessage = "核对失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanAdjust()
        {
            int amount;
            return !IsBusy && SelectedItem != null && int.TryParse(ActualAmount, out amount) && amount >= 0 &&
                !string.IsNullOrWhiteSpace(AdjustmentRemark);
        }

        private async Task AdjustAsync()
        {
            int amount;
            if (!int.TryParse(ActualAmount, out amount) || SelectedItem == null) return;
            if (!confirmation.Confirm($"确认将产品 {SelectedItem.ProductID} 的库存调整为 {amount}？此操作会生成不可删除的盘点流水。")) return;

            var adjusted = false;
            IsBusy = true;
            try
            {
                var item = SelectedItem;
                var number = await Task.Run(() => transactionService.AdjustInventory(new InventoryAdjustmentRequest
                {
                    StoreHouseID = item.StoreHouseID ?? 0,
                    ProductID = item.ProductID ?? 0,
                    BarCode = item.BarCode,
                    ActualAmount = amount,
                    Remark = AdjustmentRemark
                }));
                AdjustmentRemark = string.Empty;
                StatusMessage = "盘点调整已提交，流水号：" + number;
                adjusted = true;
            }
            catch (Exception ex)
            {
                StatusMessage = "盘点调整失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            if (adjusted) await RefreshAsync();
        }
    }
}
