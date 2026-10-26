using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Configuration;
using System;
using System.Windows.Input;

namespace Psdr.Desktop.ViewModels.Account
{
    /// <summary>
    /// Edits the agent number used to locate the current tenant database.
    /// </summary>
    public sealed class ConnectionSettingsWindowViewModel : BindableBase
    {
        private readonly AgentSettingsStore settings;
        private string agentNo;
        private string statusMessage;

        /// <summary>
        /// 初始化ConnectionSettingsWindowViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public ConnectionSettingsWindowViewModel(AgentSettingsStore settings)
        {
            this.settings = settings;
            agentNo = settings.Load();
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public event EventHandler RequestClose;
        public string AgentNo { get => agentNo; set => SetProperty(ref agentNo, value); }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 校验代理编号并保存为当前工作站要连接的代理库。
        /// </summary>
        private void Save()
        {
            try
            {
                settings.Save(AgentNo);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (ArgumentException ex)
            {
                StatusMessage = ex.Message;
            }
            catch (Exception)
            {
                StatusMessage = "保存失败，请检查设置文件的写入权限。";
            }
        }
    }
}
