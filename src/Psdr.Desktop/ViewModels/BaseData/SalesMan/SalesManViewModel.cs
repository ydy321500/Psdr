using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.SalesMan;
using Psdr.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.SalesMan
{
        /// <summary>Coordinates salesperson list and editor state.</summary>
        public sealed class SalesManViewModel : BindableBase
        {
        private readonly SalesManDAL dal;
        private readonly IConfirmationService confirmation;
        private List<Psdr.Models.Entities.SalesMan> all = new List<Psdr.Models.Entities.SalesMan>();
        private ObservableCollection<Psdr.Models.Entities.SalesMan> items = new ObservableCollection<Psdr.Models.Entities.SalesMan>();
        private Psdr.Models.Entities.SalesMan selectedItem;
        private string searchText;
        private string statusMessage = "正在准备销售人员数据…";
        private bool isEditing;
        private bool isBusy;
        private int editingId;
        private string name;
        private bool isActive;
        
        /// <summary>
        /// 初始化SalesManViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public SalesManViewModel(SalesManDAL dal, IConfirmationService confirmation)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(StartNew, () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.SalesMan>(item =>
            {
                SelectedItem = item;
                StartEdit();
            }, item => !IsBusy && item != null);
            DeleteCommand = new DelegateCommand<Psdr.Models.Entities.SalesMan>(async item =>
            {
                SelectedItem = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<Psdr.Models.Entities.SalesMan> Items
        {
            get => items;
            private set
            {
                if (!SetProperty(ref items, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
        public Psdr.Models.Entities.SalesMan SelectedItem
        {
            get => selectedItem;
            set { if (SetProperty(ref selectedItem, value)) { EditCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); } }
        }
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public string EditorTitle => editingId == 0 ? "新增销售人员" : "编辑销售人员";
        public string Name { get => name; set => SetProperty(ref name, value); }
        public bool IsActive { get => isActive; set => SetProperty(ref isActive, value); }
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
        public DelegateCommand<Psdr.Models.Entities.SalesMan> EditCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.SalesMan> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 加载销售人员数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try { all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = $"共 {all.Count} 位销售人员"; }
            catch (Exception ex) { StatusMessage = $"读取失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 根据当前搜索条件筛选列表数据。
        /// </summary>
        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            Items = new ObservableCollection<Psdr.Models.Entities.SalesMan>(string.IsNullOrEmpty(keyword) ? all :
                all.Where(x => (x.Name ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        /// <summary>
        /// 初始化新建数据的编辑状态。
        /// </summary>
        private void StartNew()
        {
            editingId = 0;
            Name = string.Empty;
            IsActive = true;
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
            editingId = item.SalesID;
            Name = item.Name;
            IsActive = item.State == 1;
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            if (IsBusy || !IsEditing) return;
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "销售人员姓名为必填项。"; return; }
            var item = new Psdr.Models.Entities.SalesMan
            {
                SalesID = editingId, Name = Name.Trim(), State = IsActive ? 1 : 0
            };
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Save(item))) { StatusMessage = "保存失败：姓名已存在，或记录已被删除。"; return; }
                IsEditing = false;
                all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = "销售人员已保存。";
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
            if (IsBusy || item == null || !confirmation.Confirm($"确认删除销售人员“{item.Name}”？")) return;
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Delete(item.SalesID))) { StatusMessage = "删除失败：记录可能已被删除。"; return; }
                all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = "销售人员已删除。";
            }
            catch (Exception ex) { StatusMessage = $"删除失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
