using Prism.Ioc;
using Psdr.Desktop.Views;
using System.Windows;

namespace Psdr.Desktop.Services
{
    /// <summary>
    /// Changes the application's main window after a successful login.
    /// </summary>
    public sealed class ApplicationSessionService : IApplicationSessionService
    {
        private readonly IContainerProvider container;
        private readonly CurrentSession session;

        /// <summary>
        /// 初始化。
        /// </summary>
        public ApplicationSessionService(IContainerProvider container, CurrentSession session)
        {
            this.container = container;
            this.session = session;
        }

        /// <summary>
        /// 创建并显示业务主窗体，同时关闭登录窗体。
        /// </summary>
        public void OpenMainWindow(Psdr.Models.Entities.User user)
        {
            session.SignIn(user);
            var login = Application.Current.MainWindow;
            var shell = container.Resolve<Shell>();
            Application.Current.MainWindow = shell;
            shell.Show();
            login.Close();
        }
    }
}
