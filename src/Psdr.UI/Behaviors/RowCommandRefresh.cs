using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Psdr.UI.Behaviors
{
    /// <summary>
    /// Re-evaluates row commands after a recycled DataGrid cell receives its row parameter.
    /// </summary>
    public static class RowCommandRefresh
    {
        public static readonly DependencyProperty OnLoadedProperty = DependencyProperty.RegisterAttached(
            "OnLoaded", typeof(bool), typeof(RowCommandRefresh), new PropertyMetadata(false, OnLoadedChanged));

        /// <summary>
        /// 获取行加载刷新行为的附加属性值。
        /// </summary>
        public static bool GetOnLoaded(DependencyObject element) => (bool)element.GetValue(OnLoadedProperty);

        /// <summary>
        /// 设置行加载刷新行为的附加属性值。
        /// </summary>
        public static void SetOnLoaded(DependencyObject element, bool value) =>
            element.SetValue(OnLoadedProperty, value);

        /// <summary>
        /// 响应行加载刷新属性变化。
        /// </summary>
        private static void OnLoadedChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
        {
            if (!(element is ButtonBase button)) return;
            button.Loaded -= RefreshCommand;
            if ((bool)args.NewValue) button.Loaded += RefreshCommand;
        }

        /// <summary>
        /// 响应行加载事件并执行绑定命令的刷新操作。
        /// </summary>
        private static void RefreshCommand(object sender, RoutedEventArgs args)
        {
            var button = (ButtonBase)sender;
            button.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (!button.IsLoaded || button.Command == null) return;
                var command = button.Command;
                button.SetCurrentValue(ButtonBase.CommandProperty, null);
                button.SetCurrentValue(ButtonBase.CommandProperty, command);
            }));
        }
    }
}
