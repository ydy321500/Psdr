using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Psdr.Desktop.Services;
using Psdr.Desktop.ViewModels.BaseData.StoreHouse;
using Psdr.Desktop.ViewModels.BaseData.Product;
using Psdr.Desktop.ViewModels.Stock;
using Psdr.Desktop.ViewModels.Finance;
using Psdr.Desktop.ViewModels.Set;
using Psdr.Desktop.ViewModels.Market;
using Psdr.Desktop.ViewModels.Report;
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
        private bool isSettingsGroupExpanded;
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
            NavigateProductCommand = new DelegateCommand(NavigateProduct);
            NavigateBarCodeGenerationCommand = new DelegateCommand(NavigateBarCodeGeneration);
            NavigateStockCommand = new DelegateCommand(NavigateStock);
            NavigateStockInCommand = new DelegateCommand(NavigateStockIn);
            NavigateStockIssueCommand = new DelegateCommand(NavigateStockIssue);
            NavigateSalesOrderReviewCommand = new DelegateCommand(NavigateSalesOrderReview);
            NavigateStockReturnCommand = new DelegateCommand(NavigateStockReturn);
            NavigateBusinessDocumentCommand = new DelegateCommand(NavigateBusinessDocument);
            NavigateFinanceCommand = new DelegateCommand(NavigateFinance);
            NavigatePersonPriceCommand = new DelegateCommand(NavigatePersonPrice);
            NavigateSystemSettingCommand = new DelegateCommand(NavigateSystemSetting);
            NavigateMarketProductCommand = new DelegateCommand(NavigateMarketProduct);
            NavigateMarketUserCommand = new DelegateCommand(NavigateMarketUser);
            NavigateMarketIndentCommand = new DelegateCommand(NavigateMarketIndent);
            NavigateOperationLogCommand = new DelegateCommand(NavigateOperationLog);
            NavigateInventoryReportCommand = new DelegateCommand(NavigateInventoryReport);
            NavigateReportStyleCommand = new DelegateCommand(NavigateReportStyle);
            NavigateBarCodeBindingCommand = new DelegateCommand(NavigateBarCodeBinding);
            NavigateInventoryCheckCommand = new DelegateCommand(NavigateInventoryCheck);
            NavigateBarcodeInventoryCommand = new DelegateCommand(NavigateBarcodeInventory);
            NavigateRedPacketCommand = new DelegateCommand(NavigateRedPacket);
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
                RaisePropertyChanged(nameof(IsProductActive));
                RaisePropertyChanged(nameof(IsBarCodeGenerationActive));
                RaisePropertyChanged(nameof(IsStockActive));
                RaisePropertyChanged(nameof(IsStockInActive));
                RaisePropertyChanged(nameof(IsStockIssueActive));
                RaisePropertyChanged(nameof(IsSalesOrderReviewActive));
                RaisePropertyChanged(nameof(IsStockReturnActive));
                RaisePropertyChanged(nameof(IsBusinessDocumentActive));
                RaisePropertyChanged(nameof(IsFinanceActive));
                RaisePropertyChanged(nameof(IsPersonPriceActive));
                RaisePropertyChanged(nameof(IsSystemSettingActive));
                RaisePropertyChanged(nameof(IsMarketProductActive));
                RaisePropertyChanged(nameof(IsMarketUserActive));
                RaisePropertyChanged(nameof(IsMarketIndentActive));
                RaisePropertyChanged(nameof(IsOperationLogActive));
                RaisePropertyChanged(nameof(IsInventoryReportActive));
                RaisePropertyChanged(nameof(IsReportStyleActive));
                RaisePropertyChanged(nameof(IsBarCodeBindingActive));
                RaisePropertyChanged(nameof(IsInventoryCheckActive));
                RaisePropertyChanged(nameof(IsBarcodeInventoryActive));
                RaisePropertyChanged(nameof(IsRedPacketActive));
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
        public bool IsProductActive => SelectedTab?.Key == "product";
        public bool IsBarCodeGenerationActive => SelectedTab?.Key == "barcodegeneration";
        public bool IsStockActive => SelectedTab?.Key == "stock";
        public bool IsStockInActive => SelectedTab?.Key == "stockin";
        public bool IsStockIssueActive => SelectedTab?.Key == "stockissue";
        public bool IsSalesOrderReviewActive => SelectedTab?.Key == "salesorderreview";
        public bool IsStockReturnActive => SelectedTab?.Key == "stockreturn";
        public bool IsBusinessDocumentActive => SelectedTab?.Key == "businessdocument";
        public bool IsFinanceActive => SelectedTab?.Key == "finance";
        public bool IsPersonPriceActive => SelectedTab?.Key == "personprice";
        public bool IsSystemSettingActive => SelectedTab?.Key == "systemsetting";
        public bool IsMarketProductActive => SelectedTab?.Key == "marketproduct";
        public bool IsMarketUserActive => SelectedTab?.Key == "marketuser";
        public bool IsMarketIndentActive => SelectedTab?.Key == "marketindent";
        public bool IsOperationLogActive => SelectedTab?.Key == "operationlog";
        public bool IsInventoryReportActive => SelectedTab?.Key == "inventoryreport";
        public bool IsReportStyleActive => SelectedTab?.Key == "reportstyle";
        public bool IsBarCodeBindingActive => SelectedTab?.Key == "barcodebinding";
        public bool IsInventoryCheckActive => SelectedTab?.Key == "inventorycheck";
        public bool IsBarcodeInventoryActive => SelectedTab?.Key == "barcodeinventory";
        public bool IsRedPacketActive => SelectedTab?.Key == "redpacket";
        public bool IsSupplyActive => SelectedTab?.Key == "supply";
        public bool IsShopActive => SelectedTab?.Key == "shop";
        public bool IsSalesManActive => SelectedTab?.Key == "salesman";
        public bool IsUserActive => SelectedTab?.Key == "user";
        public bool IsProductMapActive => SelectedTab?.Key == "productmap";
        public bool IsDictionaryActive => SelectedTab?.Key == "dictionary";
        public bool CanStoreHouse => currentSession.CanAccess(BusinessPermissions.StoreHouse);
        public bool CanProduct => currentSession.CanAccess(BusinessPermissions.Product);
        public bool CanBarCodeGeneration => currentSession.CanAccess(BusinessPermissions.BarCodeGeneration);
        public bool CanStock => currentSession.CanAccess(BusinessPermissions.Stock);
        public bool CanStockIn => currentSession.CanAccess(BusinessPermissions.StockIn);
        public bool CanStockIssue => currentSession.CanAccess(BusinessPermissions.StockIssue);
        public bool CanSalesOrderReview => currentSession.CanAccess(BusinessPermissions.StockIssue);
        public bool CanStockReturn => currentSession.CanAccess(BusinessPermissions.StockReturn);
        public bool CanBusinessDocument => currentSession.CanAccess(BusinessPermissions.BusinessDocument);
        public bool CanFinance => currentSession.CanAccess(BusinessPermissions.Finance);
        public bool CanPersonPrice => currentSession.CanAccess(BusinessPermissions.PersonPrice);
        public bool CanSystemSetting => currentSession.CanAccess(BusinessPermissions.SystemSetting);
        public bool CanMarketProduct => currentSession.CanAccess(BusinessPermissions.MarketProduct);
        public bool CanMarketUser => currentSession.CanAccess(BusinessPermissions.MarketUser);
        public bool CanMarketIndent => currentSession.CanAccess(BusinessPermissions.MarketIndent);
        public bool CanOperationLog => currentSession.CanAccess(BusinessPermissions.OperationLog);
        public bool CanInventoryReport => currentSession.CanAccess(BusinessPermissions.InventoryReport);
        public bool CanReportStyle => currentSession.CanAccess(BusinessPermissions.ReportStyle);
        public bool CanBarCodeBinding => currentSession.CanAccess(BusinessPermissions.BarCodeBinding);
        public bool CanInventoryCheck => currentSession.CanAccess(BusinessPermissions.InventoryCheck);
        public bool CanBarcodeInventory => currentSession.CanAccess(BusinessPermissions.BarcodeInventory);
        public bool CanRedPacket => currentSession.CanAccess(BusinessPermissions.RedPacket);
        public bool CanSupply => currentSession.CanAccess(BusinessPermissions.Supplier);
        public bool CanShop => currentSession.CanAccess(BusinessPermissions.Customer);
        public bool CanSalesMan => currentSession.CanAccess(BusinessPermissions.SalesMan);
        public bool CanManageUsers => currentSession.IsBusinessAdministrator;
        public bool CanProductMap => currentSession.CanAccess(BusinessPermissions.ProductMap);
        public bool CanDictionary => currentSession.CanAccess(BusinessPermissions.Dictionary);
        public bool HasBaseDataAccess => CanStoreHouse || CanProduct || CanBarCodeGeneration || CanStock || CanStockIn || CanStockIssue || CanSalesOrderReview || CanStockReturn || CanBusinessDocument || CanFinance || CanPersonPrice || CanBarCodeBinding || CanInventoryCheck || CanSupply || CanShop || CanSalesMan
            || CanDictionary || CanManageUsers || CanMarketProduct || CanMarketUser || CanMarketIndent || CanInventoryReport || CanReportStyle || CanRedPacket || CanBarcodeInventory;
        public bool HasSettingsAccess => CanProductMap || CanSystemSetting || CanOperationLog;
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateStoreHouseCommand { get; }
        public ICommand NavigateProductCommand { get; }
        public ICommand NavigateBarCodeGenerationCommand { get; }
        public ICommand NavigateStockCommand { get; }
        public ICommand NavigateStockInCommand { get; }
        public ICommand NavigateStockIssueCommand { get; }
        public ICommand NavigateSalesOrderReviewCommand { get; }
        public ICommand NavigateStockReturnCommand { get; }
        public ICommand NavigateBusinessDocumentCommand { get; }
        public ICommand NavigateFinanceCommand { get; }
        public ICommand NavigatePersonPriceCommand { get; }
        public ICommand NavigateSystemSettingCommand { get; }
        public ICommand NavigateMarketProductCommand { get; }
        public ICommand NavigateMarketUserCommand { get; }
        public ICommand NavigateMarketIndentCommand { get; }
        public ICommand NavigateOperationLogCommand { get; }
        public ICommand NavigateInventoryReportCommand { get; }
        public ICommand NavigateReportStyleCommand { get; }
        public ICommand NavigateBarCodeBindingCommand { get; }
        public ICommand NavigateInventoryCheckCommand { get; }
        public ICommand NavigateBarcodeInventoryCommand { get; }
        public ICommand NavigateRedPacketCommand { get; }
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
        /// 导航到产品资料维护页面。
        /// </summary>
        private void NavigateProduct()
        {
            if (!CanProduct) return;
            CloseCompactMenus();
            OpenTab("product", "产品资料", () =>
            {
                var viewModel = container.Resolve<ProductViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到代理条码生成页面。
        /// </summary>
        private void NavigateBarCodeGeneration()
        {
            if (!CanBarCodeGeneration) return;
            CloseCompactMenus();
            OpenTab("barcodegeneration", "条码生成", () =>
            {
                var viewModel = container.Resolve<BarCodeViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到库存查询页面。
        /// </summary>
        private void NavigateStock()
        {
            if (!CanStock) return;
            CloseCompactMenus();
            OpenTab("stock", "库存查询", () =>
            {
                var viewModel = container.Resolve<StockViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到采购入库页面。
        /// </summary>
        private void NavigateStockIn()
        {
            if (!CanStockIn) return;
            CloseCompactMenus();
            OpenTab("stockin", "采购入库", () =>
            {
                var viewModel = container.Resolve<StockInViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到销售出库页面。
        /// </summary>
        private void NavigateStockIssue()
        {
            if (!CanStockIssue) return;
            CloseCompactMenus();
            OpenTab("stockissue", "销售出库", () =>
            {
                var viewModel = container.Resolve<StockIssueViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>导航到待审核销售订单页面。</summary>
        private void NavigateSalesOrderReview()
        {
            if (!CanSalesOrderReview) return;
            CloseCompactMenus();
            OpenTab("salesorderreview", "订单审核", () =>
            {
                var viewModel = container.Resolve<SalesOrderReviewViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到客户退货页面。
        /// </summary>
        private void NavigateStockReturn()
        {
            if (!CanStockReturn) return;
            CloseCompactMenus();
            OpenTab("stockreturn", "客户退货", () =>
            {
                var viewModel = container.Resolve<StockReturnViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到业务单据查询页面。
        /// </summary>
        private void NavigateBusinessDocument()
        {
            if (!CanBusinessDocument) return;
            CloseCompactMenus();
            OpenTab("businessdocument", "业务单据", () =>
            {
                var viewModel = container.Resolve<BusinessDocumentViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到财务往来页面。
        /// </summary>
        private void NavigateFinance()
        {
            if (!CanFinance) return;
            CloseCompactMenus();
            OpenTab("finance", "财务往来", () =>
            {
                var viewModel = container.Resolve<FinanceViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到客户和供应商专属价格页面。
        /// </summary>
        private void NavigatePersonPrice()
        {
            if (!CanPersonPrice) return;
            CloseCompactMenus();
            OpenTab("personprice", "专属价格", () =>
            {
                var viewModel = container.Resolve<PersonPriceViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到系统配置页面。
        /// </summary>
        private void NavigateSystemSetting()
        {
            if (!CanSystemSetting) return;
            CloseCompactMenus();
            OpenTab("systemsetting", "系统设置", () =>
            {
                var viewModel = container.Resolve<IniViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>导航到市场产品页面。</summary>
        private void NavigateMarketProduct()
        {
            if (!CanMarketProduct) return;
            CloseCompactMenus();
            OpenTab("marketproduct", "市场产品", () => { var viewModel = container.Resolve<MarketProductViewModel>(); viewModel.Load(); return viewModel; }, true);
        }

        /// <summary>
        /// 导航到市场会员管理页面。
        /// </summary>
        private void NavigateMarketUser()
        {
            if (!CanMarketUser) return;
            CloseCompactMenus();
            OpenTab("marketuser", "市场会员", () =>
            {
                var viewModel = container.Resolve<MarketUserViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到市场订单审核页面。
        /// </summary>
        private void NavigateMarketIndent()
        {
            if (!CanMarketIndent) return;
            CloseCompactMenus();
            OpenTab("marketindent", "市场订单", () =>
            {
                var viewModel = container.Resolve<MarketIndentViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到当前代理库的操作日志页面。
        /// </summary>
        private void NavigateOperationLog()
        {
            if (!CanOperationLog) return;
            CloseCompactMenus();
            OpenTab("operationlog", "操作日志", () =>
            {
                var viewModel = container.Resolve<LogViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到按仓库和产品汇总的库存报表页面。
        /// </summary>
        private void NavigateInventoryReport()
        {
            if (!CanInventoryReport) return;
            CloseCompactMenus();
            OpenTab("inventoryreport", "库存汇总报表", () =>
            {
                var viewModel = container.Resolve<InventoryReportViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到报表模板管理页面。
        /// </summary>
        private void NavigateReportStyle()
        {
            if (!CanReportStyle) return;
            CloseCompactMenus();
            OpenTab("reportstyle", "报表模板", () =>
            {
                var viewModel = container.Resolve<ReportStyleViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到条码绑定页面。
        /// </summary>
        private void NavigateBarCodeBinding()
        {
            if (!CanBarCodeBinding) return;
            CloseCompactMenus();
            OpenTab("barcodebinding", "条码绑定", () =>
            {
                var viewModel = container.Resolve<BarCodeBindingViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到库存核对页面。
        /// </summary>
        private void NavigateInventoryCheck()
        {
            if (!CanInventoryCheck) return;
            CloseCompactMenus();
            OpenTab("inventorycheck", "库存核对", () =>
            {
                var viewModel = container.Resolve<InventoryCheckViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到有条码库存的扫码盘点页面。
        /// </summary>
        private void NavigateBarcodeInventory()
        {
            if (!CanBarcodeInventory) return;
            CloseCompactMenus();
            OpenTab("barcodeinventory", "有码扫码盘点", () =>
            {
                var viewModel = container.Resolve<BarcodeInventoryViewModel>();
                viewModel.Load();
                return viewModel;
            }, true);
        }

        /// <summary>
        /// 导航到已售条码的红包兑换状态页面。
        /// </summary>
        private void NavigateRedPacket()
        {
            if (!CanRedPacket) return;
            CloseCompactMenus();
            OpenTab("redpacket", "红包兑换状态", () =>
            {
                var viewModel = container.Resolve<RedPacketViewModel>();
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
