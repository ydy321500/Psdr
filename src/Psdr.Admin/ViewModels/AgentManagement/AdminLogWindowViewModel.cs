using Prism.Commands;
using Prism.Mvvm;
using Psdr.Admin.Services;
using Psdr.Dals.Admin;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace Psdr.Admin.ViewModels.AgentManagement
{
    /// <summary>
    /// 显示主库管理操作日志。
    /// </summary>
    public sealed class AdminLogWindowViewModel : BindableBase
    {
        private readonly AdminLogDAL dal;
        private string status = "正在读取管理日志…";
        private bool isBusy;

        /// <summary>
        /// 初始化管理日志窗口并校验管理员登录状态。
        /// </summary>
        public AdminLogWindowViewModel(AdminLogDAL dal, AdminSession session)
        {
            if (!session.IsAuthenticated) throw new InvalidOperationException("请先登录管理员账号。");
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            CloseCommand = new DelegateCommand<Window>(window => window?.Close());
            RefreshCommand.Execute();
        }

        public ObservableCollection<ServerLog> Items { get; } = new ObservableCollection<ServerLog>();
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) RefreshCommand.RaiseCanExecuteChanged(); } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<Window> CloseCommand { get; }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var records = await Task.Run(() => dal.GetRecent(500));
                Items.Clear();
                foreach (var record in records) Items.Add(record);
                Status = $"最近 {Items.Count} 条主库审计日志。";
            }
            catch (Exception ex)
            {
                Status = "读取管理日志失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
