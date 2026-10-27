using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Psdr.UI.Views;

namespace Psdr.UI.Behaviors
{
    /// <summary>
    /// 将页面中的编辑表单临时移入模态小窗，保留原有绑定和保存命令。
    /// </summary>
    public static class EditorDialog
    {
        private sealed class EditorState
        {
            public Panel Parent;
            public int Index;
            public EditorDialogWindow Window;
            public bool IsClosing;
            public Visibility OriginalVisibility;
            public bool WasSaved;
            public TextBlock Heading;
            public Visibility HeadingVisibility;
        }

        private static readonly Dictionary<FrameworkElement, EditorState> States =
            new Dictionary<FrameworkElement, EditorState>();

        public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
            "IsOpen", typeof(bool), typeof(EditorDialog),
            new PropertyMetadata(false, OnIsOpenChanged));

        public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(
            "Title", typeof(string), typeof(EditorDialog), new PropertyMetadata("编辑资料"));

        public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.RegisterAttached(
            "CancelCommand", typeof(ICommand), typeof(EditorDialog));

        public static readonly DependencyProperty TargetProperty = DependencyProperty.RegisterAttached(
            "Target", typeof(FrameworkElement), typeof(EditorDialog), new PropertyMetadata(null, OnTargetChanged));

        public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached(
            "Width", typeof(double), typeof(EditorDialog), new PropertyMetadata(500.0));

        public static readonly DependencyProperty StatusProperty = DependencyProperty.RegisterAttached(
            "Status", typeof(string), typeof(EditorDialog), new PropertyMetadata(null, OnStatusChanged));

        public static readonly DependencyProperty SuccessTextProperty = DependencyProperty.RegisterAttached(
            "SuccessText", typeof(string), typeof(EditorDialog), new PropertyMetadata(null));

        /// <summary>读取编辑操作的状态消息。</summary>
        public static string GetStatus(DependencyObject element) => (string)element.GetValue(StatusProperty);

        /// <summary>绑定编辑操作的状态消息。</summary>
        public static void SetStatus(DependencyObject element, string value) => element.SetValue(StatusProperty, value);

        /// <summary>读取表示保存成功的消息前缀。</summary>
        public static string GetSuccessText(DependencyObject element) => (string)element.GetValue(SuccessTextProperty);

        /// <summary>设置表示保存成功的消息前缀。</summary>
        public static void SetSuccessText(DependencyObject element, string value) => element.SetValue(SuccessTextProperty, value);

        /// <summary>仅在保存成功的状态消息出现后关闭编辑窗。</summary>
        private static void OnStatusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (!(sender is FrameworkElement editor) || !States.TryGetValue(editor, out var state)) return;
            var prefix = GetSuccessText(editor);
            if (!string.IsNullOrEmpty(prefix) && args.NewValue is string message &&
                Array.Exists(prefix.Split('|'), x => message.IndexOf(x, StringComparison.Ordinal) >= 0) &&
                !state.IsClosing)
            {
                state.WasSaved = true;
                state.Window?.Close();
            }
        }

        /// <summary>读取编辑小窗宽度。</summary>
        public static double GetWidth(DependencyObject element) => (double)element.GetValue(WidthProperty);

        /// <summary>为需要多列明细的编辑窗指定宽度。</summary>
        public static void SetWidth(DependencyObject element, double value) => element.SetValue(WidthProperty, value);

        /// <summary>读取按钮或列表要打开的编辑表单。</summary>
        public static FrameworkElement GetTarget(DependencyObject element) =>
            (FrameworkElement)element.GetValue(TargetProperty);

        /// <summary>指定按钮或列表要打开的编辑表单。</summary>
        public static void SetTarget(DependencyObject element, FrameworkElement value) =>
            element.SetValue(TargetProperty, value);

        /// <summary>读取编辑窗是否打开。</summary>
        public static bool GetIsOpen(DependencyObject element) => (bool)element.GetValue(IsOpenProperty);

        /// <summary>通过数据绑定打开或关闭编辑窗。</summary>
        public static void SetIsOpen(DependencyObject element, bool value) => element.SetValue(IsOpenProperty, value);

        /// <summary>读取编辑窗标题。</summary>
        public static string GetTitle(DependencyObject element) => (string)element.GetValue(TitleProperty);

        /// <summary>设置编辑窗标题。</summary>
        public static void SetTitle(DependencyObject element, string value) => element.SetValue(TitleProperty, value);

        /// <summary>读取用户关闭小窗时调用的取消命令。</summary>
        public static ICommand GetCancelCommand(DependencyObject element) => (ICommand)element.GetValue(CancelCommandProperty);

        /// <summary>设置用户关闭小窗时调用的取消命令。</summary>
        public static void SetCancelCommand(DependencyObject element, ICommand value) => element.SetValue(CancelCommandProperty, value);

        /// <summary>
        /// 给新增按钮和列表选中项接入同一个编辑小窗入口。
        /// </summary>
        private static void OnTargetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is Button button)
            {
                button.Click -= OpenFromButton;
                if (args.NewValue != null) button.Click += OpenFromButton;
            }
            else if (sender is DataGrid grid)
            {
                grid.PreviewMouseLeftButtonDown -= OpenFromRowClick;
                grid.KeyDown -= OpenFromEnter;
                if (args.NewValue != null)
                {
                    grid.PreviewMouseLeftButtonDown += OpenFromRowClick;
                    grid.KeyDown += OpenFromEnter;
                }
            }
        }

        /// <summary>在新增命令运行后显示编辑表单。</summary>
        private static void OpenFromButton(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var editor = GetTarget(button);
            button.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Open(editor)));
        }

        /// <summary>用户单击记录行后显示编辑表单。</summary>
        private static void OpenFromRowClick(object sender, MouseButtonEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (ItemsControl.ContainerFromElement(grid, e.OriginalSource as DependencyObject) is DataGridRow)
                grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => Open(GetTarget(grid))));
        }

        /// <summary>键盘用户按回车时打开当前记录的编辑表单。</summary>
        private static void OpenFromEnter(object sender, KeyEventArgs e)
        {
            var grid = (DataGrid)sender;
            if (e.Key != Key.Enter || grid.SelectedItem == null) return;
            e.Handled = true;
            Open(GetTarget(grid));
        }

        /// <summary>
        /// 根据编辑状态打开小窗，保存结束后将表单放回原位置。
        /// </summary>
        private static void OnIsOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (!(sender is FrameworkElement editor)) return;
            if ((bool)args.NewValue)
            {
                if (!editor.IsLoaded)
                {
                    RoutedEventHandler loaded = null;
                    loaded = (s, e) =>
                    {
                        editor.Loaded -= loaded;
                        if (GetIsOpen(editor)) QueueOpen(editor);
                    };
                    editor.Loaded += loaded;
                }
                else QueueOpen(editor);
            }
            else if (States.TryGetValue(editor, out var state) && state.Window != null && !state.IsClosing)
            {
                state.Window.Close();
            }
        }

        /// <summary>等待编辑状态和表单绑定完成后再创建小窗。</summary>
        private static void QueueOpen(FrameworkElement editor)
        {
            editor.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => { if (GetIsOpen(editor)) Open(editor); }));
        }

        /// <summary>
        /// 从页面取出表单并在宿主窗体上方显示模态编辑窗。
        /// </summary>
        private static void Open(FrameworkElement editor)
        {
            if (editor == null || States.ContainsKey(editor) || !(editor.Parent is Panel parent)) return;
            var owner = Window.GetWindow(editor);
            var state = new EditorState
            {
                Parent = parent,
                Index = parent.Children.IndexOf(editor),
                OriginalVisibility = editor.Visibility
            };
            States.Add(editor, state);
            var heading = FindFirstTextBlock(editor);
            if (heading != null && ReferenceEquals(heading.Style,
                Application.Current?.TryFindResource("PageTitleTextStyle")))
            {
                state.Heading = heading;
                state.HeadingVisibility = heading.Visibility;
                heading.Visibility = Visibility.Collapsed;
            }
            editor.DataContext = parent.DataContext;
            parent.Children.Remove(editor);
            editor.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
            var window = new EditorDialogWindow(GetTitle(editor), editor)
            {
                Owner = owner,
                DataContext = parent.DataContext,
                Width = GetWidth(editor)
            };
            state.Window = window;
            window.Closed += (s, e) =>
            {
                state.IsClosing = true;
                window.ReleaseEditor();
                if (state.Heading != null) state.Heading.Visibility = state.HeadingVisibility;
                parent.Children.Insert(Math.Min(state.Index, parent.Children.Count), editor);
                editor.SetCurrentValue(UIElement.VisibilityProperty,
                    GetIsOpen(editor) ? state.OriginalVisibility : Visibility.Collapsed);
                States.Remove(editor);
                if (GetIsOpen(editor) || (!state.WasSaved && GetCancelCommand(editor) != null))
                {
                    var cancel = GetCancelCommand(editor);
                    if (cancel?.CanExecute(null) == true) cancel.Execute(null);
                }
                state.IsClosing = false;
            };
            window.ShowDialog();
        }

        /// <summary>读取表单中的第一个文本元素，以识别与弹窗标题重复的页内标题。</summary>
        private static TextBlock FindFirstTextBlock(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is TextBlock textBlock) return textBlock;
                if (child is DependencyObject element)
                {
                    var result = FindFirstTextBlock(element);
                    if (result != null) return result;
                }
            }
            return null;
        }
    }
}
