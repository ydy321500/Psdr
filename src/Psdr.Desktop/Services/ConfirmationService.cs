using System.Windows;
using Psdr.Desktop.Views.Common;

namespace Psdr.Desktop.Services
{
    public sealed class ConfirmationService : IConfirmationService
    {
        /// <summary>
        /// 显示确认对话框并返回用户选择结果。
        /// </summary>
        public bool Confirm(string message)
        {
            var dialog = new ConfirmationWindow(message)
            {
                Owner = Application.Current.MainWindow
            };
            return dialog.ShowDialog() == true;
        }
    }
}
