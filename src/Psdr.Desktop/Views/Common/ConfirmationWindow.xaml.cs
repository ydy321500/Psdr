using System.Windows;

namespace Psdr.Desktop.Views.Common
{
    /// <summary>
    /// A presentation-only confirmation dialog shared by business modules.
    /// </summary>
    public partial class ConfirmationWindow : Window
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public ConfirmationWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        /// <summary>
        /// 确认当前操作并关闭确认窗口。
        /// </summary>
        private void ConfirmClick(object sender, RoutedEventArgs args)
        {
            DialogResult = true;
        }

        /// <summary>
        /// 取消当前操作并关闭确认窗口。
        /// </summary>
        private void CancelClick(object sender, RoutedEventArgs args)
        {
            DialogResult = false;
        }
    }
}
