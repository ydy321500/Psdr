using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Psdr.Desktop.Services;
using Psdr.Desktop.ViewModels.BaseData.StoreHouse;
using Psdr.Desktop.ViewModels.BaseData.Supply;
using Psdr.Desktop.ViewModels.BaseData.Shop;
using Psdr.Desktop.ViewModels.BaseData.SalesMan;
using Psdr.Desktop.ViewModels.BaseData.User;
using Psdr.Desktop.ViewModels.Set.ProductMap;
using Psdr.Desktop.ViewModels.BaseData.Dictionary;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Psdr.Desktop.ViewModels
{
    /// <summary>
    /// One open workspace page and its retained view model.
    /// </summary>
    public sealed class WorkspaceTab
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public WorkspaceTab(string key, string title, object content, bool canClose)
        {
            Key = key;
            Title = title;
            Content = content;
            CanClose = canClose;
        }

        public string Key { get; }
        public string Title { get; }
        public object Content { get; }
        public bool CanClose { get; }
    }

    /// <summary>
    /// Owns the shell, sidebar and multi-page workspace state.
    /// </summary>
    public sealed class ShellViewModel : BindableBase
    {
        private readonly IContainerProvider container;
        private readonly IWindowCommandService windowCommands;
        private readonly IConfirmationService confirmation;
        private readonly CurrentSession currentSession;
        private string pageTitle = "系统看板";
        private bool isSidebarVisible = true;
        private bool isSidebarExpanded = true;
        private bool isBaseDataGroupExpanded = true;
        private bool isSettingsGroupExpanded = true;
        private bool isCompactBaseDataMenuOpen;
        private bool isCompactSettingsMenuOpen;
        private bool isUserMenuOpen;
        private WorkspaceTab selectedTab;

        /// <summary>
        /// 初始化ShellViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public ShellViewModel(IContainerProvider container, IWindowCommandService windowCommands,
            IConfirmationService confirmation, CurrentSession currentSession)
        {
            this.container = container;
            this.windowCommands = windowCommands;
            this.confirmation = confirmation;
            this.currentSession = currentSession;
            Tabs = new ObservableCollection<WorkspaceTab>();
            NavigateHomeCommand = new DelegateCommand(NavigateHome);
            NavigateStoreHouseCommand = new DelegateCommand(NavigateStoreHouse);
            NavigateSupplyCommand = new DelegateCommand(NavigateSupply);
            NavigateShopCommand = new DelegateCommand(NavigateShop);
            NavigateSalesManCommand = new DelegateCommand(NavigateSalesMan);
            NavigateUserCommand = new DelegateCommand(NavigateUser);
            NavigateProductMapCommand = new DelegateCommand(NavigateProductMap);
            NavigateDictionaryCommand = new DelegateCommand(NavigateDictionary);
            CloseTabCommand = new DelegateCommand<WorkspaceTab>(CloseTab);
            ToggleSidebarVisibilityCommand = new DelegateCommand(ToggleSidebarVisibility);
            ToggleSidebarExpandedCommand = new DelegateCommand(ToggleSidebarExpanded);
            ToggleBaseDataGroupCommand = new DelegateCommand(ToggleBaseDataGroup);
            ToggleSettingsGroupCommand = new DelegateCommand(() =>
                IsSettingsGroupExpanded = !IsSettingsGroupExpanded);
            MinimizeCommand = new DelegateCommand(windowCommands.Minimize);
            ToggleMaximizeCommand = new DelegateCommand(windowCommands.ToggleMaximize);
            CloseCommand = new DelegateCommand(windowCommands.Close);
            NavigateHome();
        }

        public ObservableCollection<WorkspaceTab> Tabs { get; }

        public WorkspaceTab SelectedTab
        {
            get => selectedTab;
            set
            {
                if (!SetProperty(ref selectedTab, value)) return;
                PageTitle = value?.Title ?? "系统看板";
                RaisePropertyChanged(nameof(IsHomeActive));
                RaisePropertyChanged(nameof(IsStoreHouseActive));
                RaisePropertyChanged(nameof(IsSupplyActive));
                RaisePropertyChanged(nameof(IsShopActive));
                RaisePropertyChanged(nameof(IsSalesManActive));
                RaisePropertyChanged(nameof(IsUserActive));
                RaisePropertyChanged(nameof(IsProductMapActive));
                RaisePropertyChanged(nameof(IsDictionaryActive));
            }
        }

        public string PageTitle { get => pageTitle; private set => SetProperty(ref pageTitle, value); }
        public bool IsHomeActive => SelectedTab?.Key == "home";
        public bool IsStoreHouseActive => SelectedTab?.Key == "storehouse";
        public bool IsSupplyActive => SelectedTab?.Key == "supply";
        public bool IsShopActive => SelectedTab?.Key == "shop";
        public bool IsSalesManActive => SelectedTab?.Key == "salesman";
        public bool IsUserActive => SelectedTab?.Key == "user";
        public bool IsProductMapActive => SelectedTab?.Key == "productmap";
        public bool IsDictionaryActive => SelectedTab?.Key == "dictionary";
        public bool CanStoreHouse => currentSession.CanAccess(BusinessPermissions.StoreHouse);
        public bool CanSupply => currentSession.CanAccess(BusinessPermissions.Supplier);
        public bool CanShop => currentSession.CanAccess(BusinessPermissions.Customer);
        public bool CanSalesMan => currentSession.CanAccess(BusinessPermissions.SalesMan);
        public bool CanManageUsers => currentSession.IsBusinessAdministrator;
        public bool CanProductMap => currentSession.CanAccess(BusinessPermissions.ProductMap);
        public bool CanDictionary => currentSession.CanAccess(BusinessPermissions.Dictionary);
        public bool HasBaseDataAccess => CanStoreHouse || CanSupply || CanShop || CanSalesMan
            || CanDictionary || CanManageUsers;
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateStoreHouseCommand { get; }
        public ICommand NavigateSupplyCommand { get; }
        public ICommand NavigateShopCommand { get; }
        public ICommand NavigateSalesManCommand { get; }
        public ICommand NavigateUserCommand { get; }
        public ICommand NavigateProductMapCommand { get; }
        public ICommand NavigateDictionaryCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand ToggleSidebarVisibilityCommand { get; }
        public ICommand ToggleSidebarExpandedCommand { get; }
        public ICommand ToggleBaseDataGroupCommand { get; }
        public ICommand ToggleSettingsGroupCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand ToggleMaximizeCommand { get; }
        public ICommand CloseCommand { get; }

        public string CurrentUserName => currentSession.DisplayName ?? "未登录";
        public string CurrentUserCode => currentSession.UserCode ?? string.Empty;
        public string CurrentUserRole => string.IsNullOrWhiteSpace(currentSession.Role)
            ? "未设置角色" : currentSession.Role.TrimEnd('|');
        public string CurrentUserInitial => string.IsNullOrEmpty(CurrentUserName)
            ? "U" : CurrentUserName.Substring(0, 1).ToUpperInvariant();

        /// <summary>
        /// 询问用户是否确认退出应用程序。
        /// </summary>
        public bool ConfirmApplicationExit()
        {
            return confirmation.Confirm("确定要退出进销存红包系统吗？");
        }

        public GridLength SidebarWidth => !IsSidebarVisible
            ? new GridLength(0)
            : new GridLength(IsSidebarExpanded ? 250 : 72);

        public bool IsSidebarVisible
        {
            get => isSidebarVisible;
            private set
            {
                if (!SetProperty(ref isSidebarVisible, value)) return;
                RaisePropertyChanged(nameof(SidebarWidth));
                RaisePropertyChanged(nameof(IsSidebarCompact));
            }
        }

        public bool IsSidebarExpanded
        {
            get => isSidebarExpanded;
            private set
            {
                if (!SetProperty(ref isSidebarExpanded, value)) return;
                RaisePropertyChanged(nameof(SidebarWidth));
                RaisePropertyChanged(nameof(IsSidebarCompact));
            }
        }

        public bool IsSidebarCompact => IsSidebarVisible && !IsSidebarExpanded;
        public bool IsBaseDataGroupExpanded
        {
            get => isBaseDataGroupExpanded;
            private set => SetProperty(ref isBaseDataGroupExpanded, value);
        }
        public bool IsSettingsGroupExpanded
        {
            get => isSettingsGroupExpanded;
            private set => SetProperty(ref isSettingsGroupExpanded, value);
        }
        public bool IsCompactBaseDataMenuOpen
        {
            get => isCompactBaseDataMenuOpen;
            set
            {
                if (SetProperty(ref isCompactBaseDataMenuOpen, value) && value)
                    IsCompactSettingsMenuOpen = false;
            }
        }
        public bool IsCompactSettingsMenuOpen
        {
            get => isCompactSettingsMenuOpen;
            set
            {
                if (SetProperty(ref isCompactSettingsMenuOpen, value) && value)
                    IsCompactBaseDataMenuOpen = false;
            }
        }
        public bool IsUserMenuOpen { get => isUserMenuOpen; set => SetProperty(ref isUserMenuOpen, value); }

        /// <summary>
        /// 关闭侧边栏中的紧凑菜单。
        /// </summary>
        private void CloseCompactMenus()
        {
            IsCompactBaseDataMenuOpen = false;
            IsCompactSettingsMenuOpen = false;
        }

        /// <summary>
        /// 导航到首页看板。
        /// </summary>
        private void NavigateHome()
        {
            CloseCompactMenus();
            OpenTab("home", "系统看板", () => container.Resolve<HomeViewModel>(), false);
            (SelectedTab?.Content as HomeViewModel)?.Load();
        }

        /// <summary>
        /// 导航到仓库维护页面。
        /// </summary>
        private void NavigateStoreHouse()
        {
            if (!CanStoreHouse) return;
            CloseCompactMenus();
            OpenTab("storehouse", "仓库维护", () =>
            {
                var viewModel = container.Resolve<StoreHouseViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到供应商维护页面。
        /// </summary>
        private void NavigateSupply()
        {
            if (!CanSupply) return;
            CloseCompactMenus();
            OpenTab("supply", "供应商维护", () =>
            {
                var viewModel = container.Resolve<SupplyViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到客户维护页面。
        /// </summary>
        private void NavigateShop()
        {
            if (!CanShop) return;
            CloseCompactMenus();
            OpenTab("shop", "客户维护", () =>
            {
                var viewModel = container.Resolve<ShopViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到销售人员维护页面。
        /// </summary>
        private void NavigateSalesMan()
        {
            if (!CanSalesMan) return;
            CloseCompactMenus();
            OpenTab("salesman", "销售人员维护", () =>
            {
                var viewModel = container.Resolve<SalesManViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到用户维护页面。
        /// </summary>
        private void NavigateUser()
        {
            if (!CanManageUsers) return;
            CloseCompactMenus();
            OpenTab("user", "用户维护", () =>
            {
                var viewModel = container.Resolve<UserViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到产品属性维护页面。
        /// </summary>
        private void NavigateProductMap()
        {
            if (!CanProductMap) return;
            CloseCompactMenus();
            OpenTab("productmap", "产品属性", () =>
            {
                var viewModel = container.Resolve<ProductMapViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到字典维护页面。
        /// </summary>
        private void NavigateDictionary()
        {
            if (!CanDictionary) return;
            CloseCompactMenus();
            OpenTab("dictionary", "字典维护", () =>
            {
                var viewModel = container.Resolve<DictionaryViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 创建并打开一个工作区标签页。
        /// </summary>
        private void OpenTab(string key, string title, System.Func<object> createContent, bool canClose)
        {
            var tab = Tabs.FirstOrDefault(item => item.Key == key);
            if (tab == null)
            {
                tab = new WorkspaceTab(key, title, createContent(), canClose);
                Tabs.Add(tab);
            }
            SelectedTab = tab;
        }

        /// <summary>
        /// 关闭指定的工作区标签页。
        /// </summary>
        private void CloseTab(WorkspaceTab tab)
        {
            if (tab == null || !tab.CanClose || !Tabs.Contains(tab)) return;
            var wasSelected = SelectedTab == tab;
            Tabs.Remove(tab);
            if (wasSelected) SelectedTab = Tabs.LastOrDefault();
        }

        /// <summary>
        /// 切换侧边栏的显示状态。
        /// </summary>
        private void ToggleSidebarVisibility()
        {
            CloseCompactMenus();
            IsSidebarVisible = !IsSidebarVisible;
        }

        /// <summary>
        /// 切换侧边栏的展开状态。
        /// </summary>
        private void ToggleSidebarExpanded()
        {
            CloseCompactMenus();
            IsSidebarExpanded = !IsSidebarExpanded;
        }
        /// <summary>
        /// 切换基础数据菜单组的展开状态。
        /// </summary>
        private void ToggleBaseDataGroup() { IsBaseDataGroupExpanded = !IsBaseDataGroupExpanded; }
    }
}
