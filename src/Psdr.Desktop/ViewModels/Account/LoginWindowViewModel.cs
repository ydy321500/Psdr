using Prism.Commands;
using Prism.Mvvm;
using Psdr.Admin.Services;
using Psdr.Dals.Admin;
using Psdr.Dals.Configuration;
using Psdr.Dals.User;
using Psdr.Desktop.Services;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Psdr.Desktop.ViewModels.Account
{
    /// <summary>
    /// Validates the business account and opens the Prism shell.
    /// </summary>
    public sealed class LoginWindowViewModel : BindableBase
    {
        private readonly UserDAL users;
        private readonly IApplicationSessionService session;
        private readonly IWindowCommandService windowCommands;
        private readonly LoginPreferenceStore preferences;
        private readonly IConnectionSettingsDialogService settingsDialog;
        private readonly AgentSettingsStore agentSettings;
        private readonly AdminAuthService adminAuthentication;
        private readonly CentralDatabase centralDatabase;
        private readonly AdminWindowNavigator adminNavigator;
        private string userCode;
        private SecureString password;
        private string errorMessage;
        private bool isBusy;
        private bool rememberAccount;

        /// <summary>
        /// 初始化LoginWindowViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public LoginWindowViewModel(UserDAL users, IApplicationSessionService session,
            IWindowCommandService windowCommands, LoginPreferenceStore preferences,
            IConnectionSettingsDialogService settingsDialog,
            AgentSettingsStore agentSettings, AdminAuthService adminAuthentication,
            CentralDatabase centralDatabase, AdminWindowNavigator adminNavigator)
        {
            this.users = users;
            this.session = session;
            this.windowCommands = windowCommands;
            this.preferences = preferences;
            this.settingsDialog = settingsDialog;
            this.agentSettings = agentSettings;
            this.adminAuthentication = adminAuthentication;
            this.centralDatabase = centralDatabase;
            this.adminNavigator = adminNavigator;
            var saved = preferences.Load();
            rememberAccount = saved.RememberAccount;
            userCode = saved.RememberAccount ? saved.UserCode : null;
            LoginCommand = new DelegateCommand(async () => await LoginAsync(), () => !IsBusy)
                .ObservesProperty(() => IsBusy);
            CloseCommand = new DelegateCommand(windowCommands.Close);
            MinimizeCommand = new DelegateCommand(windowCommands.Minimize);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);
        }

        public string UserCode
        {
            get => userCode;
            set => SetProperty(ref userCode, value);
        }

        public SecureString Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            private set => SetProperty(ref errorMessage, value);
        }

        public bool RememberAccount
        {
            get => rememberAccount;
            set => SetProperty(ref rememberAccount, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set => SetProperty(ref isBusy, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand OpenSettingsCommand { get; }

        /// <summary>
        /// 完成。
        /// </summary>
        private void OpenSettings()
        {
            settingsDialog.Show();
        }

        /// <summary>
        /// 完成。
        /// </summary>
        private async Task LoginAsync()
        {
            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(UserCode) || Password == null || Password.Length == 0)
            {
                ErrorMessage = "请输入账号和密码。";
                return;
            }

            var code = UserCode.Trim();
            var pointer = IntPtr.Zero;
            string secret;
            try
            {
                pointer = Marshal.SecureStringToBSTR(Password);
                secret = Marshal.PtrToStringBSTR(pointer);
            }
            finally
            {
                if (pointer != IntPtr.Zero) Marshal.ZeroFreeBSTR(pointer);
            }

            IsBusy = true;
            try
            {
                if (string.Equals(code, "sys", StringComparison.OrdinalIgnoreCase))
                {
                    var adminName = await Task.Run(() => adminAuthentication.Authenticate(code, secret));
                    if (adminName == null)
                    {
                        ErrorMessage = "管理员账号或密码错误。";
                        return;
                    }
                    await Task.Run(() => centralDatabase.EnsureProvisioningRights());
                    preferences.Save(RememberAccount, code);
                    adminNavigator.OpenManagement(adminName);
                    return;
                }

                if (!Guid.TryParse(agentSettings.Load(), out _))
                {
                    ErrorMessage = "请先在代理设置中输入有效的代理号。";
                    return;
                }

                var user = await Task.Run(() => users.Authenticate(code, secret));
                if (user == null)
                {
                    ErrorMessage = "账号或密码错误，或账号已停用。";
                    return;
                }
                preferences.Save(RememberAccount, code);
                session.OpenMainWindow(user);
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (CryptographicException)
            {
                ErrorMessage = "本机主库查询凭据无法解密，请运行主库初始化脚本。";
            }
            catch (Exception)
            {
                ErrorMessage = "登录失败，请检查代理库连接与账号配置。";
            }
            finally
            {
                secret = null;
                IsBusy = false;
            }
        }
    }
}
