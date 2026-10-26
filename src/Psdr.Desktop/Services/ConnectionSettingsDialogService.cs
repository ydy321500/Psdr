using Prism.Ioc;
using Psdr.Desktop.Views.Account;
using System.Windows;

namespace Psdr.Desktop.Services
{
    public sealed class ConnectionSettingsDialogService : IConnectionSettingsDialogService
    {
        private readonly IContainerProvider container;

        /// <summary>
        /// 初始化。
        /// </summary>
        public ConnectionSettingsDialogService(IContainerProvider container)
        {
            this.container = container;
        }

        /// <summary>
        /// 打开连接设置对话框。
        /// </summary>
        public void Show()
        {
            var window = container.Resolve<ConnectionSettingsWindow>();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}
