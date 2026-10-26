using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Stock;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Stock
{
    /// <summary>
    /// 提供库存浏览和关键字筛选，不直接修改库存数量。
    /// </summary>
    public sealed class StockViewModel : BindableBase
    {
        private readonly StockDAL dal;
        private List<StockListItem> all = new List<StockListItem>();
        private ObservableCollection<StockListItem> items = new ObservableCollection<StockListItem>();
        private ObservableCollection<StockLedgerListItem> ledgerItems = new ObservableCollection<StockLedgerListItem>();
        private string searchText;
        private string statusMessage = "正在读取库存…";
        private bool isBusy;

        /// <summary>
        /// 初始化库存查询页面的刷新命令。
        /// </summary>
        public StockViewModel(StockDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
        }

        public ObservableCollection<StockListItem> Items { get => items; private set => SetProperty(ref items, value); }
        public ObservableCollection<StockLedgerListItem> LedgerItems { get => ledgerItems; private set => SetProperty(ref ledgerItems, value); }
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) RefreshCommand.RaiseCanExecuteChanged(); } }
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 加载库存数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var data = await Task.Run(() => new { Stock = dal.GetAll(), HasLedger = dal.HasStockLedger(), Ledger = dal.GetRecentLedger() });
                all = data.Stock; LedgerItems = new ObservableCollection<StockLedgerListItem>(data.Ledger);
                ApplyFilter();
                StatusMessage = data.HasLedger
                    ? $"共 {all.Count} 条库存记录，最近 {LedgerItems.Count} 条库存流水。"
                    : $"共 {all.Count} 条库存记录。当前代理库为旧结构，未启用库存流水；系统没有自动修改该代理库。";
            }
            catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            Items = new ObservableCollection<StockListItem>(string.IsNullOrEmpty(keyword) ? all : all.Where(x =>
                Contains(x.ProductType, keyword) || Contains(x.ProductAttribute1, keyword) ||
                Contains(x.ProductAttribute2, keyword) || Contains(x.BarCode, keyword) || Contains(x.Position, keyword) ||
                Contains(x.StoreHouseName, keyword)));
        }

        private static bool Contains(string value, string keyword) => (value ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
