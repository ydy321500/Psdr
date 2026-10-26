using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Stock;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>
    /// 展示售出条码的红包兑换状态，便于处理客户咨询与退货判断。
    /// </summary>
    public sealed class RedPacketViewModel : BindableBase
    {
        private readonly RedPacketDAL dal;
        private string status = "正在读取红包兑换记录…";
        private bool isBusy;

        /// <summary>
        /// 初始化红包状态查询页。
        /// </summary>
        public RedPacketViewModel(RedPacketDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
        }

        public ObservableCollection<RedPacketSaleItem> Items { get; } =
            new ObservableCollection<RedPacketSaleItem>();

        public string Status { get => status; private set => SetProperty(ref status, value); }
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
        /// 页面打开时读取最近售出条码。
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
                var records = await Task.Run(() => dal.GetRecent(500));
                Items.Clear();
                foreach (var record in records) Items.Add(record);
                Status = $"最近 {Items.Count} 条已售条码。";
            }
            catch (Exception ex)
            {
                Status = "读取红包状态失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
