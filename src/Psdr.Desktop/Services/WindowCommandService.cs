using System.Windows;

namespace Psdr.Desktop.Services
{
    public sealed class WindowCommandService : IWindowCommandService
    {
        /// <summary>
        /// 将当前窗体最小化。
        /// </summary>
        public void Minimize()
        {
            var window = Application.Current.MainWindow;
            if (window != null) window.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 切换当前窗体的最大化和还原状态。
        /// </summary>
        public void ToggleMaximize()
        {
            var window = Application.Current.MainWindow;
            if (window == null) return;
            window.WindowState = window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        /// <summary>
        /// 关闭当前窗体。
        /// </summary>
        public void Close()
        {
            Application.Current.MainWindow?.Close();
        }
    }
}
