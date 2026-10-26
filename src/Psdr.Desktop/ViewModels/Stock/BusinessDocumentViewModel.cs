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
    /// <summary>提供采购、销售和退货单据的统一浏览和筛选。</summary>
    public sealed class BusinessDocumentViewModel : BindableBase
    {
        private readonly BusinessDocumentDAL dal;
        private List<BusinessDocumentListItem> all = new List<BusinessDocumentListItem>();
        private ObservableCollection<BusinessDocumentListItem> items = new ObservableCollection<BusinessDocumentListItem>();
        private ObservableCollection<BusinessDocumentLineItem> lines = new ObservableCollection<BusinessDocumentLineItem>();
        private BusinessDocumentListItem selectedItem;
        private string searchText;
        private string selectedDocumentType = "全部";
        private DateTime? startDate;
        private DateTime? endDate;
        private string statusMessage = "正在读取业务单据…";
        private bool isBusy;

        /// <summary>初始化单据查询页面。</summary>
        public BusinessDocumentViewModel(BusinessDocumentDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
        }
        public ObservableCollection<BusinessDocumentListItem> Items { get => items; private set => SetProperty(ref items, value); }
        public ObservableCollection<BusinessDocumentLineItem> Lines { get => lines; private set => SetProperty(ref lines, value); }
        public BusinessDocumentListItem SelectedItem
        {
            get => selectedItem;
            set
            {
                if (SetProperty(ref selectedItem, value)) _ = LoadLinesAsync(value);
            }
        }
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string[] DocumentTypes { get; } = { "全部", "采购入库", "销售出库", "客户退货" };
        public string SelectedDocumentType
        {
            get => selectedDocumentType;
            set { if (SetProperty(ref selectedDocumentType, value)) ApplyFilter(); }
        }
        public DateTime? StartDate
        {
            get => startDate;
            set { if (SetProperty(ref startDate, value)) ApplyFilter(); }
        }
        public DateTime? EndDate
        {
            get => endDate;
            set { if (SetProperty(ref endDate, value)) ApplyFilter(); }
        }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) RefreshCommand.RaiseCanExecuteChanged(); } }
        public DelegateCommand RefreshCommand { get; }
        /// <summary>加载业务单据。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync()
        {
            if (IsBusy) return; IsBusy = true;
            try { all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = $"共 {all.Count} 张业务单据。"; }
            catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            var result = all.AsEnumerable();
            if (!string.IsNullOrEmpty(SelectedDocumentType) && SelectedDocumentType != "全部")
                result = result.Where(x => x.DocumentType == SelectedDocumentType);
            if (StartDate.HasValue)
                result = result.Where(x => x.Time.HasValue && x.Time.Value.Date >= StartDate.Value.Date);
            if (EndDate.HasValue)
                result = result.Where(x => x.Time.HasValue && x.Time.Value.Date <= EndDate.Value.Date);
            if (!string.IsNullOrEmpty(keyword))
                result = result.Where(x => Contains(x.DocumentType, keyword) || Contains(x.No, keyword) ||
                    Contains(x.PartnerName, keyword) || Contains(x.ProductName, keyword) ||
                    Contains(x.StoreHouseName, keyword) || Contains(x.Remark, keyword));
            Items = new ObservableCollection<BusinessDocumentListItem>(result);
            SelectedItem = null;
        }
        private async Task LoadLinesAsync(BusinessDocumentListItem item)
        {
            if (item == null) { Lines = new ObservableCollection<BusinessDocumentLineItem>(); return; }
            try
            {
                var result = await Task.Run(() => dal.GetLines(item.DocumentType, item.No));
                if (SelectedItem != item) return;
                Lines = new ObservableCollection<BusinessDocumentLineItem>(result);
            }
            catch (Exception ex)
            {
                if (SelectedItem == item) StatusMessage = "读取单据明细失败：" + ex.Message;
            }
        }
        private static bool Contains(string value, string keyword) => (value ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }
}
