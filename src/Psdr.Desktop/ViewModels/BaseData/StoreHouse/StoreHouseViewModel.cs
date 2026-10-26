using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Psdr.Dals.StoreHouse;
using Psdr.Desktop.Services;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.StoreHouse
{
    /// <summary>
    /// Coordinates warehouse search and editing without accessing WPF controls.
    /// </summary>
    public sealed class StoreHouseViewModel : BindableBase, INavigationAware
    {
        private readonly StoreHouseDAL dal;
        private readonly IConfirmationService confirmation;
        private List<Psdr.Models.Entities.StoreHouse> allItems = new List<Psdr.Models.Entities.StoreHouse>();
        private ObservableCollection<Psdr.Models.Entities.StoreHouse> items =
            new ObservableCollection<Psdr.Models.Entities.StoreHouse>();
        private Psdr.Models.Entities.StoreHouse selectedItem;
        private string searchText;
        private string editorName;
        private string editorRemark;
        private string statusMessage = "正在准备仓库数据…";
        private bool isEditing;
        private bool isBusy;
        private int editingId;

        /// <summary>
        /// 初始化StoreHouseViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public StoreHouseViewModel(StoreHouseDAL dal, IConfirmationService confirmation)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(StartNew, () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.StoreHouse>(item =>
            {
                SelectedItem = item;
                StartEdit();
            }, item => !IsBusy && item != null);
            DeleteCommand = new DelegateCommand<Psdr.Models.Entities.StoreHouse>(async item =>
            {
                SelectedItem = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(CancelEdit);
        }

        public ObservableCollection<Psdr.Models.Entities.StoreHouse> Items
        {
            get => items;
            private set
            {
                if (!SetProperty(ref items, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public Psdr.Models.Entities.StoreHouse SelectedItem
        {
            get => selectedItem;
            set
            {
                if (SetProperty(ref selectedItem, value))
                {
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (SetProperty(ref searchText, value)) ApplyFilter();
            }
        }

        public string EditorName
        {
            get => editorName;
            set => SetProperty(ref editorName, value);
        }

        public string EditorRemark
        {
            get => editorRemark;
            set => SetProperty(ref editorRemark, value);
        }

        public string EditorTitle => editingId == 0 ? "新增仓库" : "编辑仓库";

        public string StatusMessage
        {
            get => statusMessage;
            private set => SetProperty(ref statusMessage, value);
        }

        public bool IsEditing
        {
            get => isEditing;
            private set
            {
                if (SetProperty(ref isEditing, value)) SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged();
                NewCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.StoreHouse> EditCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.StoreHouse> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 处理页面导航进入事件并刷新数据。
        /// </summary>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Load();
        }

        /// <summary>
        /// 加载仓库数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 判断当前页面是否可以复用。
        /// </summary>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 处理页面导航离开事件。
        /// </summary>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "正在读取仓库…";
            try
            {
                allItems = await Task.Run(() => dal.GetAll());
                ApplyFilter();
                StatusMessage = $"共 {allItems.Count} 个仓库";
            }
            catch (Exception ex)
            {
                StatusMessage = $"读取失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 根据当前搜索条件筛选列表数据。
        /// </summary>
        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            var filtered = string.IsNullOrEmpty(keyword)
                ? allItems
                : allItems.Where(x => (x.Name ?? string.Empty).IndexOf(keyword,
                    StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
            Items = new ObservableCollection<Psdr.Models.Entities.StoreHouse>(filtered);
        }

        /// <summary>
        /// 初始化新建数据的编辑状态。
        /// </summary>
        private void StartNew()
        {
            editingId = 0;
            EditorName = string.Empty;
            EditorRemark = string.Empty;
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 将选中数据加载到编辑区。
        /// </summary>
        private void StartEdit()
        {
            if (SelectedItem == null) return;
            editingId = SelectedItem.StoreHouseID;
            EditorName = SelectedItem.Name;
            EditorRemark = SelectedItem.Remark;
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 取消当前编辑并恢复原始数据。
        /// </summary>
        private void CancelEdit()
        {
            IsEditing = false;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            if (IsBusy || !IsEditing) return;
            var name = (EditorName ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                StatusMessage = "请输入仓库名称。";
                return;
            }
            var item = new Psdr.Models.Entities.StoreHouse
            {
                StoreHouseID = editingId,
                Name = name,
                Remark = (EditorRemark ?? string.Empty).Trim()
            };
            IsBusy = true;
            try
            {
                var saved = await Task.Run(() => dal.Save(item));
                if (!saved)
                {
                    StatusMessage = "保存失败：仓库名称已存在，或记录已被删除。";
                    return;
                }
                IsEditing = false;
                allItems = await Task.Run(() => dal.GetAll());
                ApplyFilter();
                StatusMessage = "仓库已保存。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步删除当前选中的数据记录。
        /// </summary>
        private async Task DeleteAsync()
        {
            if (IsBusy || SelectedItem == null) return;
            var item = SelectedItem;
            if (!confirmation.Confirm($"确认删除仓库“{item.Name}”？")) return;
            IsBusy = true;
            try
            {
                var deleted = await Task.Run(() => dal.Delete(item.StoreHouseID));
                if (!deleted)
                {
                    StatusMessage = "删除失败：记录可能已被删除。";
                    return;
                }
                allItems = await Task.Run(() => dal.GetAll());
                ApplyFilter();
                StatusMessage = "仓库已删除。";
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败：{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
