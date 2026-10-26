using Prism.Ioc;
using Prism.Mvvm;
using Psdr.Admin.Views.AgentManagement;
using System.Windows;

namespace Psdr.Admin.Services
{
    public sealed class AdminWindowNavigator
    {
        private readonly IContainerProvider container;
        private readonly AdminSession session;

        /// <summary>
        /// 初始化。
        /// </summary>
        public AdminWindowNavigator(IContainerProvider container, AdminSession session)
        {
            this.container = container;
            this.session = session;
        }

        /// <summary>
        /// 打开代理及功能权限管理窗口。
        /// </summary>
        public void OpenManagement(string displayName)
        {
            session.SignIn(displayName);
            var login = Application.Current.MainWindow;
            var management = container.Resolve<AgentManagementWindow>();
            ViewModelLocator.SetAutoWireViewModel(management, true);
            Application.Current.MainWindow = management;
            management.Show();
            login.Close();
        }
    }
}
