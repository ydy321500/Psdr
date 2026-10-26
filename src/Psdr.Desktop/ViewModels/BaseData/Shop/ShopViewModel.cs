using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Shop;
using Psdr.Dals.Configuration;
using Psdr.Desktop.Services;
using Psdr.Desktop.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.Shop
{
        /// <summary>Coordinates customer list and editor state.</summary>
        public sealed class ShopViewModel : BindableBase
        {
        private readonly ShopDAL dal;
        private readonly IConfirmationService confirmation;
        private List<Psdr.Models.Entities.Shop> all = new List<Psdr.Models.Entities.Shop>();
        private ObservableCollection<Psdr.Models.Entities.Shop> items = new ObservableCollection<Psdr.Models.Entities.Shop>();
        private List<string> salesmanNames = new List<string>();
        private Psdr.Models.Entities.Shop selectedItem;
        private string searchText;
        private string statusMessage = "正在准备客户数据…";
        private bool isEditing;
        private bool isBusy;
        private int editingId;
        private string name;
        private string address;
        private string contact;
        private string telephone;
        private string mobile;
        private string fax;
        private string salesman;
        private string remark;
        
        /// <summary>
        /// 初始化ShopViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public ShopViewModel(ShopDAL dal, IConfirmationService confirmation, ProvinceCityCatalog catalog)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            Location = new ProvinceCitySelection(catalog);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(StartNew, () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.Shop>(item =>
            {
                SelectedItem = item;
                StartEdit();
            }, item => !IsBusy && item != null);
            DeleteCommand = new DelegateCommand<Psdr.Models.Entities.Shop>(async item =>
            {
                SelectedItem = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<Psdr.Models.Entities.Shop> Items
        {
            get => items;
            private set
            {
                if (!SetProperty(ref items, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
        public List<string> SalesmanNames { get => salesmanNames; private set => SetProperty(ref salesmanNames, value); }
        public Psdr.Models.Entities.Shop SelectedItem
        {
            get => selectedItem;
            set { if (SetProperty(ref selectedItem, value)) { EditCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); } }
        }
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public string EditorTitle => editingId == 0 ? "新增客户" : "编辑客户";
        public string Name { get => name; set => SetProperty(ref name, value); }
        public ProvinceCitySelection Location { get; }
        public string Address { get => address; set => SetProperty(ref address, value); }
        public string Contact { get => contact; set => SetProperty(ref contact, value); }
        public string Telephone { get => telephone; set => SetProperty(ref telephone, value); }
        public string Mobile { get => mobile; set => SetProperty(ref mobile, value); }
        public string Fax { get => fax; set => SetProperty(ref fax, value); }
        public string Salesman { get => salesman; set => SetProperty(ref salesman, value); }
        public string Remark { get => remark; set => SetProperty(ref remark, value); }
        public bool IsEditing { get => isEditing; private set { if (SetProperty(ref isEditing, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged(); NewCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged();
            }
        }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.Shop> EditCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.Shop> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 加载客户数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var result = await Task.Run(() => new { Shops = dal.GetAll(), Salesmen = dal.GetSalesmanNames() });
                all = result.Shops;
                SalesmanNames = result.Salesmen.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
                ApplyFilter();
                StatusMessage = $"共 {all.Count} 个客户";
            }
            catch (Exception ex) { StatusMessage = $"读取失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 根据当前搜索条件筛选列表数据。
        /// </summary>
        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            Items = new ObservableCollection<Psdr.Models.Entities.Shop>(string.IsNullOrEmpty(keyword) ? all :
                all.Where(x => (x.ShopName ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    (x.Contacts ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    (x.Salesman ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        /// <summary>
        /// 初始化新建数据的编辑状态。
        /// </summary>
        private void StartNew()
        {
            editingId = 0;
            Name = Address = Contact = Telephone = Mobile = Fax = Salesman = Remark = string.Empty;
            Location.SetValues(string.Empty, string.Empty);
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 将选中数据加载到编辑区。
        /// </summary>
        private void StartEdit()
        {
            var item = SelectedItem;
            if (item == null) return;
            editingId = item.ShopID;
            Name = item.ShopName; Location.SetValues(item.Province, item.City); Address = item.Addr;
            Contact = item.Contacts; Telephone = item.Tel; Mobile = item.Phone; Fax = item.Fax;
            Salesman = item.Salesman; Remark = item.Remark;
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            if (IsBusy || !IsEditing) return;
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Location.Province) ||
                string.IsNullOrWhiteSpace(Location.City) || string.IsNullOrWhiteSpace(Contact))
            { StatusMessage = "客户名称、省份、城市和联系人为必填项。"; return; }
            var item = new Psdr.Models.Entities.Shop
            {
                ShopID = editingId, ShopName = Name.Trim(), Province = Location.Province.Trim(),
                City = Location.City.Trim(),
                Addr = (Address ?? string.Empty).Trim(), Contacts = Contact.Trim(), Tel = (Telephone ?? string.Empty).Trim(),
                Phone = (Mobile ?? string.Empty).Trim(), Fax = (Fax ?? string.Empty).Trim(),
                Salesman = (Salesman ?? string.Empty).Trim(), Remark = (Remark ?? string.Empty).Trim()
            };
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Save(item))) { StatusMessage = "保存失败：客户名称已存在，或记录已被删除。"; return; }
                IsEditing = false;
                all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = "客户已保存。";
            }
            catch (Exception ex) { StatusMessage = $"保存失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 异步删除当前选中的数据记录。
        /// </summary>
        private async Task DeleteAsync()
        {
            var item = SelectedItem;
            if (IsBusy || item == null || !confirmation.Confirm($"确认删除客户“{item.ShopName}”？")) return;
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Delete(item.ShopID))) { StatusMessage = "删除失败：记录可能已被删除。"; return; }
                all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = "客户已删除。";
            }
            catch (Exception ex) { StatusMessage = $"删除失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
