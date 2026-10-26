using System.Windows;

namespace Psdr.Desktop.Views.Account
{
    public partial class LoginWindow : Window
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) => UserCodeBox.Focus();
        }
    }
}
