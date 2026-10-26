using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Report;
using Psdr.Desktop.Services;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Report
{
    /// <summary>
    /// 管理代理库中的报表模板文件，供后续 FastReport 打印功能加载。
    /// </summary>
    public sealed class ReportStyleViewModel : BindableBase
    {
        private readonly ReportStyleDAL dal;
        private readonly IConfirmationService confirmation;
        private ReportStyle selectedItem;
        private string reportName;
        private byte[] reportContent;
        private string sourceFileName;
        private string status = "正在读取报表模板…";
        private bool isBusy;

        /// <summary>
        /// 初始化报表模板管理页面。
        /// </summary>
        public ReportStyleViewModel(ReportStyleDAL dal, IConfirmationService confirmation)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(New, () => !IsBusy);
            ImportCommand = new DelegateCommand(Import, () => !IsBusy);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            ExportCommand = new DelegateCommand(Export, () => !IsBusy && SelectedItem?.ReportContent?.Length > 0);
            DeleteCommand = new DelegateCommand(async () => await DeleteAsync(), () => !IsBusy && SelectedItem != null);
        }

        public ObservableCollection<ReportStyle> Items { get; } = new ObservableCollection<ReportStyle>();
        public ReportStyle SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!SetProperty(ref selectedItem, value)) return;
                ReportName = value?.ReportName;
                reportContent = value?.ReportContent;
                SourceFileName = value == null ? null : "已保存模板（" + (value.ReportContent?.Length ?? 0) + " 字节）";
                RaiseCommands();
            }
        }
        public string ReportName { get => reportName; set { if (SetProperty(ref reportName, value)) RaiseCommands(); } }
        public string SourceFileName { get => sourceFileName; private set => SetProperty(ref sourceFileName, value); }
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) RaiseCommands(); } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand ImportCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand ExportCommand { get; }
        public DelegateCommand DeleteCommand { get; }

        /// <summary>
        /// 页面打开时读取模板列表。
        /// </summary>
        public void Load()
        {
            _ = RefreshAsync();
        }

        private bool CanSave()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(ReportName) && reportContent?.Length > 0;
        }

        private void New()
        {
            SelectedItem = null;
            ReportName = string.Empty;
            reportContent = null;
            SourceFileName = "未选择模板文件";
            Status = "输入模板名称并导入 .frx 文件。";
            RaiseCommands();
        }

        private void Import()
        {
            var dialog = new OpenFileDialog { Filter = "FastReport 模板 (*.frx)|*.frx|所有文件 (*.*)|*.*" };
            if (dialog.ShowDialog() != true) return;
            try
            {
                reportContent = File.ReadAllBytes(dialog.FileName);
                SourceFileName = Path.GetFileName(dialog.FileName) + "（" + reportContent.Length + " 字节）";
                if (string.IsNullOrWhiteSpace(ReportName)) ReportName = Path.GetFileNameWithoutExtension(dialog.FileName);
                Status = "已载入模板文件，点击保存后写入代理库。";
                RaiseCommands();
            }
            catch (Exception ex)
            {
                Status = "读取模板失败：" + ex.Message;
            }
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var id = SelectedItem?.ReportID;
                var records = await Task.Run(() => dal.GetAll());
                Items.Clear();
                foreach (var record in records) Items.Add(record);
                SelectedItem = id.HasValue ? Items.FirstOrDefault(x => x.ReportID == id.Value) : null;
                Status = $"共 {Items.Count} 个报表模板。";
            }
            catch (Exception ex)
            {
                Status = "读取报表模板失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!CanSave()) return;
            IsBusy = true;
            try
            {
                await Task.Run(() => dal.Save(new ReportStyle
                {
                    ReportID = SelectedItem?.ReportID ?? 0,
                    ReportName = ReportName,
                    ReportContent = reportContent
                }));
                Status = "报表模板已保存。";
            }
            catch (Exception ex)
            {
                Status = "保存报表模板失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await RefreshAsync();
        }

        private void Export()
        {
            if (SelectedItem?.ReportContent == null) return;
            var dialog = new SaveFileDialog { Filter = "FastReport 模板 (*.frx)|*.frx", FileName = SelectedItem.ReportName + ".frx" };
            if (dialog.ShowDialog() != true) return;
            try { File.WriteAllBytes(dialog.FileName, SelectedItem.ReportContent); Status = "模板已导出：" + dialog.FileName; }
            catch (Exception ex) { Status = "导出模板失败：" + ex.Message; }
        }

        private async Task DeleteAsync()
        {
            if (SelectedItem == null || !confirmation.Confirm($"确认删除报表模板“{SelectedItem.ReportName}”？")) return;
            IsBusy = true;
            try { await Task.Run(() => dal.Delete(SelectedItem.ReportID)); Status = "报表模板已删除。"; }
            catch (Exception ex) { Status = "删除报表模板失败：" + ex.Message; }
            finally { IsBusy = false; }
            await RefreshAsync();
        }

        private void RaiseCommands()
        {
            RefreshCommand.RaiseCanExecuteChanged(); NewCommand.RaiseCanExecuteChanged(); ImportCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged(); ExportCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged();
        }
    }
}
