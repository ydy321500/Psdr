using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using Psdr.Admin.Services;
using Psdr.Dals.Admin;
using Psdr.Dals.Configuration;
using Psdr.Dals.StoreHouse;
using Psdr.Dals.Supply;
using Psdr.Dals.Shop;
using Psdr.Dals.SalesMan;
using Psdr.Dals.User;
using Psdr.Dals.ProductMap;
using Psdr.Dals.Product;
using Psdr.Dals.Stock;
using Psdr.Dals.Dictionary;
using Psdr.Dals.Dashboard;
using Psdr.Dals.Finance;
using Psdr.Dals.Set;
using Psdr.Dals.Market;
using Psdr.Dals.Report;
using Psdr.Desktop.Services;
using Psdr.Desktop.Views;
using Psdr.Desktop.Views.Account;
using Psdr.Desktop.Views.BaseData.StoreHouse;
using Psdr.Desktop.Views.BaseData.Product;
using Psdr.Desktop.Views.Stock;
using Psdr.Desktop.Views.BaseData.Supply;
using Psdr.Desktop.Views.BaseData.Shop;
using Psdr.Desktop.Views.BaseData.SalesMan;
using Psdr.Desktop.Views.BaseData.User;
using Psdr.Desktop.Views.Set.ProductMap;
using Psdr.Desktop.Views.BaseData.Dictionary;
using Psdr.Desktop.Views.Finance;
using Psdr.Desktop.Views.Set;
using Psdr.Desktop.Views.Market;
using Psdr.Desktop.Views.Report;
using System.Windows;

namespace Psdr.Desktop
{
    /// <summary>
    /// Configures the desktop shell and its navigation registrations.
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>
        /// 创建并返回应用程序主 Shell 窗体。
        /// </summary>
        protected override Window CreateShell()
        {
            return Container.Resolve<LoginWindow>();
        }

        /// <summary>
        /// 向 Prism 容器注册应用程序服务、视图和数据访问组件。
        /// </summary>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<TenantDatabase>();
            containerRegistry.RegisterSingleton<ProvinceCityCatalog>();
            containerRegistry.RegisterSingleton<CentralDatabase>();
            containerRegistry.Register<AgentDAL>();
            containerRegistry.Register<AdminLogDAL>();
            containerRegistry.Register<AdminAuthService>();
            containerRegistry.Register<TenantProvisioningService>();
            containerRegistry.Register<AgentFunctionService>();
            containerRegistry.RegisterSingleton<AdminSession>();
            containerRegistry.Register<AdminWindowNavigator>();
            containerRegistry.RegisterSingleton<AgentSettingsStore>();
            containerRegistry.Register<StoreHouseDAL>();
            containerRegistry.Register<SupplyDAL>();
            containerRegistry.Register<ShopDAL>();
            containerRegistry.Register<SalesManDAL>();
            containerRegistry.Register<UserDAL>();
            containerRegistry.Register<ProductMapDAL>();
            containerRegistry.Register<ProductDAL>();
            containerRegistry.Register<BarCodeDAL>();
            containerRegistry.Register<StockDAL>();
            containerRegistry.Register<RedPacketDAL>();
            containerRegistry.Register<StockTransactionService>();
            containerRegistry.Register<BusinessDocumentDAL>();
            containerRegistry.Register<SalesOrderReviewDAL>();
            containerRegistry.Register<FinanceDAL>();
            containerRegistry.Register<PersonPriceDAL>();
            containerRegistry.Register<IniDAL>();
            containerRegistry.Register<LogDAL>();
            containerRegistry.Register<MarketProductDAL>();
            containerRegistry.Register<MarketUserDAL>();
            containerRegistry.Register<MarketIndentDAL>();
            containerRegistry.Register<InventoryReportDAL>();
            containerRegistry.Register<ReportStyleDAL>();
            containerRegistry.Register<DictionaryDAL>();
            containerRegistry.Register<DashboardDAL>();
            containerRegistry.RegisterSingleton<CurrentSession>();
            containerRegistry.RegisterSingleton<IApplicationSessionService, ApplicationSessionService>();
            containerRegistry.RegisterSingleton<LoginPreferenceStore>();
            containerRegistry.RegisterSingleton<IConnectionSettingsDialogService, ConnectionSettingsDialogService>();
            containerRegistry.RegisterSingleton<IConfirmationService, ConfirmationService>();
            containerRegistry.RegisterSingleton<IWindowCommandService, WindowCommandService>();
            containerRegistry.RegisterForNavigation<HomeView>();
            containerRegistry.RegisterForNavigation<StoreHouseView>();
            containerRegistry.RegisterForNavigation<ProductView>();
            containerRegistry.RegisterForNavigation<BarCodeView>();
            containerRegistry.RegisterForNavigation<StockView>();
            containerRegistry.RegisterForNavigation<StockInView>();
            containerRegistry.RegisterForNavigation<StockIssueView>();
            containerRegistry.RegisterForNavigation<BusinessDocumentView>();
            containerRegistry.RegisterForNavigation<SalesOrderReviewView>();
            containerRegistry.RegisterForNavigation<BarCodeBindingView>();
            containerRegistry.RegisterForNavigation<InventoryCheckView>();
            containerRegistry.RegisterForNavigation<BarcodeInventoryView>();
            containerRegistry.RegisterForNavigation<RedPacketView>();
            containerRegistry.RegisterForNavigation<FinanceView>();
            containerRegistry.RegisterForNavigation<PersonPriceView>();
            containerRegistry.RegisterForNavigation<IniView>();
            containerRegistry.RegisterForNavigation<LogView>();
            containerRegistry.RegisterForNavigation<MarketProductView>();
            containerRegistry.RegisterForNavigation<MarketUserView>();
            containerRegistry.RegisterForNavigation<MarketIndentView>();
            containerRegistry.RegisterForNavigation<InventoryReportView>();
            containerRegistry.RegisterForNavigation<ReportStyleView>();
            containerRegistry.RegisterForNavigation<SupplyView>();
            containerRegistry.RegisterForNavigation<ShopView>();
            containerRegistry.RegisterForNavigation<SalesManView>();
            containerRegistry.RegisterForNavigation<UserView>();
            containerRegistry.RegisterForNavigation<ProductMapView>();
            containerRegistry.RegisterForNavigation<DictionaryView>();
        }

        /// <summary>
        /// 完成 Prism 应用初始化并打开登录流程。
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
    }
}
