using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.User;
using Psdr.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.User
{
    public sealed class UserRoleOption : BindableBase
    {
        private bool isSelected;

        /// <summary>
        /// 初始化。
        /// </summary>
        public UserRoleOption(string name, string displayName) { Name = name; DisplayName = displayName; }
        public string Name { get; }
        public string DisplayName { get; }
        public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }
    }

        /// <summary>Maintains business user accounts in the current tenant.</summary>
        public sealed class UserViewModel : BindableBase
        {
        private readonly UserDAL dal;
        private readonly IConfirmationService confirmation;
        private readonly CurrentSession session;
        private List<Psdr.Models.Entities.User> all = new List<Psdr.Models.Entities.User>();
        private ObservableCollection<Psdr.Models.Entities.User> items = new ObservableCollection<Psdr.Models.Entities.User>();
        private Psdr.Models.Entities.User selectedItem;
        private string searchText;
        private string statusMessage = "正在准备用户数据…";
        private bool isEditing;
        private bool isBusy;
        private int editingId;
        private string code;
        private string name;
        private bool isActive;
        private string preservedRoles;
        
        /// <summary>
        /// 初始化UserViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public UserViewModel(UserDAL dal, IConfirmationService confirmation, CurrentSession session)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            this.session = session;
            Roles = new ObservableCollection<UserRoleOption>(new[]
            {
                new UserRoleOption(BusinessPermissions.StoreHouse, "仓库维护"),
                new UserRoleOption(BusinessPermissions.ProductMap, "产品属性"),
                new UserRoleOption(BusinessPermissions.Dictionary, "字典维护"),
                new UserRoleOption(BusinessPermissions.Supplier, "供应商维护"),
                new UserRoleOption(BusinessPermissions.Customer, "客户维护"),
                new UserRoleOption(BusinessPermissions.SalesMan, "销售人员维护")
            });
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(StartNew, () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.User>(item =>
            {
                SelectedItem = item;
                StartEdit();
            }, item => !IsBusy && item != null);
            DeleteCommand = new DelegateCommand<Psdr.Models.Entities.User>(async item =>
            {
                SelectedItem = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null && !string.Equals(item.UserCode, "admin", StringComparison.OrdinalIgnoreCase) && !string.Equals(item.UserCode, session.UserCode, StringComparison.OrdinalIgnoreCase));
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<Psdr.Models.Entities.User> Items
        {
            get => items;
            private set
            {
                if (!SetProperty(ref items, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
        public ObservableCollection<UserRoleOption> Roles { get; }
        public Psdr.Models.Entities.User SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!SetProperty(ref selectedItem, value)) return;
                EditCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(CanDeleteSelected));
            }
        }
        public bool CanDeleteSelected => SelectedItem != null &&
            !string.Equals(SelectedItem.UserCode, "admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(SelectedItem.UserCode, session.UserCode, StringComparison.OrdinalIgnoreCase);
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public string EditorTitle => editingId == 0 ? "新增用户" : "编辑用户";
        public string Code { get => code; set => SetProperty(ref code, value); }
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
        public DelegateCommand<Psdr.Models.Entities.User> EditCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.User> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 加载用户数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try { all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = $"共 {all.Count} 个用户"; }
            catch (Exception ex) { StatusMessage = $"读取失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 根据当前搜索条件筛选列表数据。
        /// </summary>
        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            Items = new ObservableCollection<Psdr.Models.Entities.User>(string.IsNullOrEmpty(keyword) ? all :
                all.Where(x => (x.UserCode ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    (x.UserName ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0));
        }

        /// <summary>
        /// 初始化新建数据的编辑状态。
        /// </summary>
        private void StartNew()
        {
            editingId = 0;
            Code = Name = preservedRoles = string.Empty;
            IsActive = true;
            foreach (var role in Roles) role.IsSelected = false;
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
            editingId = item.UserID;
            Code = item.UserCode;
            Name = item.UserName;
            IsActive = item.State == 1;
            var values = (item.Role ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var role in Roles) role.IsSelected = values.Contains(role.Name);
            preservedRoles = string.Join("|", values.Where(value => Roles.All(role => role.Name != value)));
            RaisePropertyChanged(nameof(EditorTitle));
            IsEditing = true;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            if (IsBusy || !IsEditing) return;
            if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
            { StatusMessage = "账号和姓名为必填项。"; return; }
            var selectedRoles = Roles.Where(role => role.IsSelected).Select(role => role.Name).ToList();
            if (!string.IsNullOrWhiteSpace(preservedRoles)) selectedRoles.Add(preservedRoles);
            if (selectedRoles.Count == 0) { StatusMessage = "请至少选择一个页面权限。"; return; }
            var item = new Psdr.Models.Entities.User
            {
                UserID = editingId, UserCode = Code.Trim(), UserName = Name.Trim(),
                Role = string.Join("|", selectedRoles) + "|", State = IsActive ? 1 : 0, Online = 0
            };
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Save(item))) { StatusMessage = "保存失败：账号已存在，或记录已被删除。"; return; }
                IsEditing = false;
                all = await Task.Run(() => dal.GetAll()); ApplyFilter();
                StatusMessage = editingId == 0 ? "用户已创建，初始密码为 123456。" : "用户已保存。";
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
            if (IsBusy || !CanDeleteSelected ||
                !confirmation.Confirm($"确认删除用户“{item.UserName}（{item.UserCode}）”？")) return;
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Delete(item.UserID))) { StatusMessage = "删除失败：记录可能已被删除。"; return; }
                all = await Task.Run(() => dal.GetAll()); ApplyFilter(); StatusMessage = "用户已删除。";
            }
            catch (Exception ex) { StatusMessage = $"删除失败：{ex.Message}"; }
            finally { IsBusy = false; }
        }
    }
}
