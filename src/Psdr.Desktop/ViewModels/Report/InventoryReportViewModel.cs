using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Report;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Report
{
    /// <summary>
    /// 展示当前代理库按仓库和产品汇总的库存报表。
    /// </summary>
    public sealed class InventoryReportViewModel : BindableBase
    {
        private readonly InventoryReportDAL dal;
        private string status = "正在读取库存汇总…";
        private bool isBusy;

        /// <summary>
        /// 初始化库存汇总报表的数据服务和刷新命令。
        /// </summary>
        public InventoryReportViewModel(InventoryReportDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
        }

        public ObservableCollection<InventoryReportItem> Items { get; } =
            new ObservableCollection<InventoryReportItem>();

        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value)) RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 页面打开时异步读取库存汇总。
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
                var records = await Task.Run(() => dal.GetInventorySummary());
                Items.Clear();
                foreach (var record in records) Items.Add(record);
                Status = $"共 {Items.Count} 个仓库产品库存项目。";
            }
            catch (Exception exception)
            {
                Status = "读取库存汇总失败：" + exception.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
