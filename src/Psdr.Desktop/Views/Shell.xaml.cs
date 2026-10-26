using Psdr.Desktop.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace Psdr.Desktop.Views
{
    /// <summary>
    /// Provides the visual container for Prism navigation.
    /// </summary>
    public partial class Shell : Window
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public Shell()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 处理主窗体关闭事件并执行退出确认。
        /// </summary>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is ShellViewModel viewModel)
                e.Cancel = !viewModel.ConfirmApplicationExit();
        }
    }
}
