using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Dashboard;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels
{
    /// <summary>
    /// Loads current-tenant figures for the system dashboard.
    /// </summary>
    public sealed class HomeViewModel : BindableBase
    {
        private readonly DashboardDAL dashboard;
        private int customerCount;
        private int supplierCount;
        private int productCount;
        private int warehouseCount;
        private int productTypeCount;
        private int maxBarValue = 1;
        private bool isBusy;
        private string statusMessage = "正在读取当前代理的业务数据…";
        private string updatedAt = "尚未更新";
        private string emptyProductTypeMessage = "暂无可用产品类型";
        private ObservableCollection<string> productTypes = new ObservableCollection<string>();

        /// <summary>
        /// 初始化HomeViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public HomeViewModel(DashboardDAL dashboard)
        {
            this.dashboard = dashboard;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
        }

        public int CustomerCount { get => customerCount; private set => SetProperty(ref customerCount, value); }
        public int SupplierCount { get => supplierCount; private set => SetProperty(ref supplierCount, value); }
        public int ProductCount { get => productCount; private set => SetProperty(ref productCount, value); }
        public int WarehouseCount { get => warehouseCount; private set => SetProperty(ref warehouseCount, value); }
        public int ProductTypeCount { get => productTypeCount; private set => SetProperty(ref productTypeCount, value); }
        public int MaxBarValue { get => maxBarValue; private set => SetProperty(ref maxBarValue, value); }
        public ObservableCollection<string> ProductTypes
        {
            get => productTypes;
            private set
            {
                if (SetProperty(ref productTypes, value)) RaisePropertyChanged(nameof(HasProductTypes));
            }
        }
        public bool HasProductTypes => ProductTypes.Count > 0;
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value)) RefreshCommand.RaiseCanExecuteChanged();
            }
        }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public string UpdatedAt { get => updatedAt; private set => SetProperty(ref updatedAt, value); }
        public string EmptyProductTypeMessage
        {
            get => emptyProductTypeMessage;
            private set => SetProperty(ref emptyProductTypeMessage, value);
        }
        public DelegateCommand RefreshCommand { get; }

        /// <summary>
        /// 加载首页看板数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusMessage = "正在更新业务概况…";
            try
            {
                var snapshot = await Task.Run(() => dashboard.GetSnapshot());
                CustomerCount = snapshot.CustomerCount;
                SupplierCount = snapshot.SupplierCount;
                ProductCount = snapshot.ProductCount;
                WarehouseCount = snapshot.WarehouseCount;
                ProductTypeCount = snapshot.ActiveProductTypes.Count;
                ProductTypes = new ObservableCollection<string>(snapshot.ActiveProductTypes.Take(8));
                EmptyProductTypeMessage = snapshot.FunctionSchemaAvailable
                    ? "暂无可用产品类型" : "产品功能尚未配置";
                MaxBarValue = Math.Max(1, new[] { CustomerCount, SupplierCount, ProductCount, WarehouseCount }.Max());
                UpdatedAt = "更新于 " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                StatusMessage = snapshot.FunctionSchemaAvailable
                    ? "数据来自当前代理数据库。"
                    : "基础资料已读取；请由主管理员配置产品功能。";
            }
            catch (Exception ex)
            {
                StatusMessage = "读取看板失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
