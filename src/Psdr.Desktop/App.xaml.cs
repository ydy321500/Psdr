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
using Psdr.Dals.Dictionary;
using Psdr.Dals.Dashboard;
using Psdr.Desktop.Services;
using Psdr.Desktop.Views;
using Psdr.Desktop.Views.Account;
using Psdr.Desktop.Views.BaseData.StoreHouse;
using Psdr.Desktop.Views.BaseData.Supply;
using Psdr.Desktop.Views.BaseData.Shop;
using Psdr.Desktop.Views.BaseData.SalesMan;
using Psdr.Desktop.Views.BaseData.User;
using Psdr.Desktop.Views.Set.ProductMap;
using Psdr.Desktop.Views.BaseData.Dictionary;
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
