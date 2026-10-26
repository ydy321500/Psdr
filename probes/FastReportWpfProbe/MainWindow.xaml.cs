using FastReport;
using System;
using System.IO;
using System.Windows;

namespace FastReportWpfProbe
{
    /// <summary>
    /// Checks whether the existing Framework report binaries work from WPF.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDesigner_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var report = new Report())
                {
                    report.Design();
                }
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            string path = TemplatePath.Text.Trim();
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "请选择存在的 .frx 模板。", "模板不存在");
                return;
            }

            try
            {
                using (var report = new Report())
                {
                    report.Load(path);
                    report.Show();
                }
            }
            catch (Exception exception)
            {
                ShowError(exception);
            }
        }

        private void ShowError(Exception exception)
        {
            MessageBox.Show(this, exception.ToString(), "FastReport 兼容性验证失败");
        }
    }
}
