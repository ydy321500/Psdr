using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace Psdr.UI.Behaviors
{
    /// <summary>
    /// Exposes a PasswordBox value to an MVVM view model without a string binding.
    /// </summary>
    public static class SecurePasswordBinding
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password", typeof(SecureString), typeof(SecurePasswordBinding),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(SecurePasswordBinding), new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// 获取 PasswordBox 中保存的安全密码值。
        /// </summary>
        public static SecureString GetPassword(DependencyObject obj) => (SecureString)obj.GetValue(PasswordProperty);
        /// <summary>
        /// 将安全密码值写入绑定的 PasswordBox。
        /// </summary>
        public static void SetPassword(DependencyObject obj, SecureString value) => obj.SetValue(PasswordProperty, value);
        /// <summary>
        /// 获取密码绑定功能的启用状态。
        /// </summary>
        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        /// <summary>
        /// 设置密码绑定功能的启用状态。
        /// </summary>
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// 响应密码绑定启用状态变化，注册或移除密码变更事件。
        /// </summary>
        private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (!(sender is PasswordBox box)) return;
            box.PasswordChanged -= OnPasswordChanged;
            if ((bool)args.NewValue) box.PasswordChanged += OnPasswordChanged;
        }

        /// <summary>
        /// 响应 PasswordBox 密码变化并同步安全密码值。
        /// </summary>
        private static void OnPasswordChanged(object sender, RoutedEventArgs args)
        {
            var box = (PasswordBox)sender;
            SetPassword(box, box.SecurePassword.Copy());
        }
    }
}
