using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Configuration;
using Psdr.Dals.Product;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.Product
{
    /// <summary>
    /// 生成条码记录，并将本次生成结果导出为可由 Excel 打开的 CSV 文件。
    /// </summary>
    public sealed class BarCodeViewModel : BindableBase
    {
        private readonly BarCodeDAL dal;
        private readonly AgentSettingsStore agentSettings;
        private string clearCodePrefix;
        private string amountText = "100";
        private string nextIndexText = "正在读取…";
        private string status = "请填写本次条码数量。";
        private bool isBusy;

        /// <summary>
        /// 初始化条码生成数据服务和操作命令。
        /// </summary>
        public BarCodeViewModel(BarCodeDAL dal, AgentSettingsStore agentSettings)
        {
            this.dal = dal;
            this.agentSettings = agentSettings;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            GenerateCommand = new DelegateCommand(async () => await GenerateAsync(), CanGenerate);
            ExportCommand = new DelegateCommand(Export, () => !IsBusy && Items.Count > 0);
        }

        public ObservableCollection<BarCode> Items { get; } = new ObservableCollection<BarCode>();

        /// <summary>
        /// 配置明码的可选访问前缀；为空时明码仅包含条码本体。
        /// </summary>
        public string ClearCodePrefix
        {
            get => clearCodePrefix;
            set => SetProperty(ref clearCodePrefix, value);
        }

        public string AmountText
        {
            get => amountText;
            set
            {
                if (SetProperty(ref amountText, value)) GenerateCommand.RaiseCanExecuteChanged();
            }
        }

        public string NextIndexText
        {
            get => nextIndexText;
            private set => SetProperty(ref nextIndexText, value);
        }

        public string Status
        {
            get => status;
            private set => SetProperty(ref status, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    GenerateCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand GenerateCommand { get; }
        public DelegateCommand ExportCommand { get; }

        /// <summary>
        /// 页面打开时读取下一个可用条码序号。
        /// </summary>
        public void Load()
        {
            _ = RefreshAsync();
        }

        private bool CanGenerate()
        {
            int amount;
            return !IsBusy && int.TryParse(AmountText, out amount) && amount > 0 && amount <= 100000;
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var max = await Task.Run(() => dal.GetMaxIndexNo());
                NextIndexText = (max + 1).ToString();
                Status = "已读取当前最大条码序号。";
            }
            catch (Exception ex)
            {
                Status = "读取条码序号失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GenerateAsync()
        {
            int amount;
            if (!int.TryParse(AmountText, out amount)) return;
            var agentNo = agentSettings.Load();
            IsBusy = true;
            try
            {
                var generated = await Task.Run(() => dal.Generate(agentNo, ClearCodePrefix, amount));
                Replace(Items, generated);
                NextIndexText = (generated[generated.Count - 1].IndexNo + 1).ToString();
                Status = $"已生成并登记 {generated.Count} 条条码，请导出后用于印刷或投放。";
                ExportCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                Status = "生成失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void Export()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV 文件 (*.csv)|*.csv",
                FileName = "条码_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                var builder = new StringBuilder("序号,明码,暗码\r\n");
                foreach (var item in Items)
                    builder.Append(item.IndexNo).Append(',').Append(Csv(item.ClearCode)).Append(',').Append(Csv(item.SecretCode)).Append("\r\n");
                File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(true));
                Status = "已导出：" + dialog.FileName;
            }
            catch (Exception ex)
            {
                Status = "导出失败：" + ex.Message;
            }
        }

        private static string Csv(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source) target.Add(item);
        }
    }
}
