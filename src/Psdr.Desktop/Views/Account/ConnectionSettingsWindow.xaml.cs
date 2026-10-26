using Psdr.Desktop.ViewModels.Account;
using System.Windows;

namespace Psdr.Desktop.Views.Account
{
    public partial class ConnectionSettingsWindow : Window
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public ConnectionSettingsWindow()
        {
            InitializeComponent();
            if (DataContext is ConnectionSettingsWindowViewModel initialViewModel)
                initialViewModel.RequestClose += CloseOnRequest;
            DataContextChanged += (sender, args) =>
            {
                if (args.OldValue is ConnectionSettingsWindowViewModel oldViewModel)
                    oldViewModel.RequestClose -= CloseOnRequest;
                if (args.NewValue is ConnectionSettingsWindowViewModel viewModel)
                    viewModel.RequestClose += CloseOnRequest;
            };
        }

        /// <summary>
        /// 完成。
        /// </summary>
        private void CloseOnRequest(object sender, System.EventArgs args)
        {
            Close();
        }
    }
}
