using System.Windows;
using System.Windows.Controls;

namespace Psdr.UI.Views
{
    /// <summary>
    /// 在自定义标题栏的小窗中承载页面已有的新增或编辑表单。
    /// </summary>
    public partial class EditorDialogWindow : Window
    {
        /// <summary>
        /// 创建编辑小窗并放入页面的表单内容。
        /// </summary>
        public EditorDialogWindow(string title, FrameworkElement editor)
        {
            InitializeComponent();
            Title = title;
            EditorHost.Content = editor;
        }

        /// <summary>
        /// 从小窗中移出表单，以便关闭后放回原页面。
        /// </summary>
        public void ReleaseEditor()
        {
            EditorHost.Content = null;
        }

        /// <summary>
        /// 响应自定义标题栏的关闭按钮。
        /// </summary>
        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
