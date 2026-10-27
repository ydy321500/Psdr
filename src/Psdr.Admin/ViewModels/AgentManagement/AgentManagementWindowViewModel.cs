using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Psdr.Admin.Services;
using Psdr.Dals.Admin;
using Psdr.Models.Entities;
using Psdr.Admin.Views.AgentManagement;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Psdr.Admin.ViewModels.AgentManagement
{
    public sealed class FunctionTypeChoice
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public FunctionTypeChoice(int value, string label) { Value = value; Label = label; }
        public int Value { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Administrator-only agent registry and database provisioning workflow.
    /// </summary>
    public sealed class AgentManagementWindowViewModel : BindableBase
    {
        private readonly AgentDAL agents;
        private readonly TenantProvisioningService provisioning;
        private readonly AgentFunctionService agentFunctions;
        private readonly AdminLogDAL adminLogs;
        private readonly IContainerProvider container;
        private readonly AdminSession session;
        private Agent selectedAgent;
        private AgentFunction selectedFunction;
        private long editingFunctionId;
        private string functionName;
        private DateTime? functionStopTime;
        private int functionType;
        private bool functionEnabled;
        private string agentName;
        private string contact;
        private string tel;
        private string address;
        private string province;
        private string city;
        private string notice;
        private string initialCredentials;
        private bool isBusy;

        /// <summary>
        /// 初始化AgentManagementWindowViewModel，设置数据依赖、界面命令和初始状态。
        /// </summary>
        public AgentManagementWindowViewModel(AgentDAL agents, TenantProvisioningService provisioning,
            AgentFunctionService agentFunctions, AdminLogDAL adminLogs,
            IContainerProvider container, AdminSession session)
        {
            if (!session.IsAuthenticated) throw new InvalidOperationException("请先登录管理员账号。" );
            this.agents = agents;
            this.provisioning = provisioning;
            this.agentFunctions = agentFunctions;
            this.adminLogs = adminLogs;
            this.container = container;
            this.session = session;
            Agents = new ObservableCollection<Agent>();
            Functions = new ObservableCollection<AgentFunction>();
            FunctionTypes = new[]
            {
                new FunctionTypeChoice(0, "产品"), new FunctionTypeChoice(1, "模块")
            };
            RefreshCommand = new DelegateCommand(async () => await RunAsync(RefreshAsync));
            NewCommand = new DelegateCommand(ClearEditor);
            CancelEditCommand = new DelegateCommand(CancelAgentEditor);
            ConfigureFunctionCommand = new DelegateCommand(ClearFunctionEditor);
            CancelFunctionCommand = new DelegateCommand(ClearFunctionEditor);
            CreateCommand = new DelegateCommand(async () => await RunAsync(CreateAsync));
            SaveCommand = new DelegateCommand(async () => await RunAsync(SaveAsync));
            RetryCommand = new DelegateCommand(async () => await RunAsync(RetryAsync));
            ToggleActiveCommand = new DelegateCommand(async () => await RunAsync(ToggleActiveAsync));
            SaveFunctionCommand = new DelegateCommand(async () => await RunAsync(SaveFunctionAsync));
            OpenLogCommand = new DelegateCommand(OpenLog);
            ClearFunctionEditor();
            MinimizeCommand = new DelegateCommand(() => SetWindowState(WindowState.Minimized));
            ToggleMaximizeCommand = new DelegateCommand(() =>
                SetWindowState(Application.Current.MainWindow.WindowState == WindowState.Maximized
                    ? WindowState.Normal : WindowState.Maximized));
            CloseCommand = new DelegateCommand(() => Application.Current.MainWindow?.Close());
            RefreshCommand.Execute();
        }

        public ObservableCollection<Agent> Agents { get; }
        public ObservableCollection<AgentFunction> Functions { get; }
        public FunctionTypeChoice[] FunctionTypes { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand CancelEditCommand { get; }
        public DelegateCommand ConfigureFunctionCommand { get; }
        public DelegateCommand CancelFunctionCommand { get; }
        public DelegateCommand CreateCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand RetryCommand { get; }
        public DelegateCommand ToggleActiveCommand { get; }
        public DelegateCommand SaveFunctionCommand { get; }
        public DelegateCommand OpenLogCommand { get; }
        public DelegateCommand MinimizeCommand { get; }
        public DelegateCommand ToggleMaximizeCommand { get; }
        public DelegateCommand CloseCommand { get; }

        public Agent SelectedAgent
        {
            get => selectedAgent;
            set
            {
                if (!SetProperty(ref selectedAgent, value)) return;
                InitialCredentials = null;
                AgentName = value?.AgentName;
                Contact = value?.Contact;
                Tel = value?.Tel;
                Address = value?.AgentAddress;
                Province = value?.AgentProvince;
                City = value?.AgentCity;
                RaisePropertyChanged(nameof(IsNew));
                RaisePropertyChanged(nameof(SelectedStatus));
                RaisePropertyChanged(nameof(SelectedId));
                RaisePropertyChanged(nameof(ToggleActiveLabel));
                RaisePropertyChanged(nameof(CanCreate));
                RaisePropertyChanged(nameof(CanSave));
                RaisePropertyChanged(nameof(CanRetry));
                RaisePropertyChanged(nameof(CanToggleActive));
                RaisePropertyChanged(nameof(CanEditFunctions));
                Functions.Clear();
                ClearFunctionEditor();
                if (value != null && value.ProvisioningStatus == "Ready")
                    _ = LoadFunctionsAsync(value.AgentID);
            }
        }

        public AgentFunction SelectedFunction
        {
            get => selectedFunction;
            set
            {
                if (!SetProperty(ref selectedFunction, value) || value == null) return;
                editingFunctionId = value.FunctionID;
                FunctionName = value.FunctionName;
                FunctionStopTime = value.StopTime;
                FunctionType = value.FunctionType;
                FunctionEnabled = value.State == 1;
                RaisePropertyChanged(nameof(CanChangeFunctionType));
            }
        }

        public string FunctionName { get => functionName; set => SetProperty(ref functionName, value); }
        public DateTime? FunctionStopTime { get => functionStopTime; set => SetProperty(ref functionStopTime, value); }
        public int FunctionType { get => functionType; set => SetProperty(ref functionType, value); }
        public bool FunctionEnabled { get => functionEnabled; set => SetProperty(ref functionEnabled, value); }
        public bool CanChangeFunctionType => editingFunctionId == 0;
        public bool CanEditFunctions => SelectedAgent != null && SelectedAgent.ProvisioningStatus == "Ready";

        public bool IsNew => SelectedAgent == null;
        public bool CanCreate => IsNew;
        public bool CanSave => !IsNew;
        public bool CanRetry => SelectedAgent != null &&
            (SelectedAgent.ProvisioningStatus == "Failed" || SelectedAgent.ProvisioningStatus == "Pending");
        public bool CanToggleActive => SelectedAgent != null && SelectedAgent.ProvisioningStatus == "Ready";
        public string SelectedStatus => SelectedAgent == null ? "尚未登记" :
            SelectedAgent.ProvisioningStatus + (string.IsNullOrEmpty(SelectedAgent.ProvisioningError)
                ? "" : " · " + SelectedAgent.ProvisioningError);
        public string SelectedId => SelectedAgent?.AgentID.ToString("D") ?? "建库后生成代理号";
        public string ToggleActiveLabel => SelectedAgent == null || SelectedAgent.State == 1
            ? "停用代理" : "启用代理";
        public string AgentName { get => agentName; set => SetProperty(ref agentName, value); }
        public string Contact { get => contact; set => SetProperty(ref contact, value); }
        public string Tel { get => tel; set => SetProperty(ref tel, value); }
        public string Address { get => address; set => SetProperty(ref address, value); }
        public string Province { get => province; set => SetProperty(ref province, value); }
        public string City { get => city; set => SetProperty(ref city, value); }
        public string Notice { get => notice; set => SetProperty(ref notice, value); }
        public string InitialCredentials { get => initialCredentials; set => SetProperty(ref initialCredentials, value); }
        public bool IsBusy { get => isBusy; set => SetProperty(ref isBusy, value); }

        /// <summary>
        /// 异步加载指定代理的功能权限列表。
        /// </summary>
        private async Task LoadFunctionsAsync(Guid agentId)
        {
            try
            {
                var result = await Task.Run(() => agentFunctions.GetAll(agentId));
                if (SelectedAgent?.AgentID != agentId) return;
                Functions.Clear();
                foreach (var function in result) Functions.Add(function);
            }
            catch (Exception ex) { Notice = "读取代理功能失败：" + ex.Message; }
        }

        /// <summary>
        /// 清空功能权限编辑区并重置编辑状态。
        /// </summary>
        private void ClearFunctionEditor()
        {
            SelectedFunction = null;
            editingFunctionId = 0;
            FunctionName = string.Empty;
            FunctionStopTime = DateTime.Today.AddYears(1);
            FunctionType = 0;
            FunctionEnabled = true;
            RaisePropertyChanged(nameof(CanChangeFunctionType));
        }

        /// <summary>
        /// 异步保存当前功能权限配置。
        /// </summary>
        private async Task SaveFunctionAsync()
        {
            if (!CanEditFunctions) throw new InvalidOperationException("请先选择已建库的代理。");
            if (!FunctionStopTime.HasValue) throw new InvalidOperationException("请选择功能到期日。");
            var request = new AgentFunction
            {
                FunctionID = editingFunctionId, AgentID = SelectedAgent.AgentID,
                FunctionName = FunctionName, StopTime = FunctionStopTime.Value.Date.AddDays(1).AddTicks(-1),
                FunctionType = FunctionType, State = FunctionEnabled ? 1 : 0
            };
            await Task.Run(() => agentFunctions.Save(request));
            await WriteLogAsync("保存代理功能", $"代理：{SelectedAgent.AgentName}；功能：{request.FunctionName}；状态：{(request.State == 1 ? "启用" : "停用")}。");
            await LoadFunctionsAsync(request.AgentID);
            ClearFunctionEditor();
            Notice = "代理功能已保存。";
        }

        /// <summary>
        /// 打开主库管理审计日志窗口。
        /// </summary>
        private void OpenLog()
        {
            var window = container.Resolve<AdminLogWindow>();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            var currentId = SelectedAgent?.AgentID;
            var result = await Task.Run(() => agents.GetAll());
            Agents.Clear();
            foreach (var agent in result) Agents.Add(agent);
            SelectedAgent = currentId.HasValue ? Agents.FirstOrDefault(x => x.AgentID == currentId.Value) : null;
            Notice = $"已加载 {Agents.Count} 个代理。";
        }

        /// <summary>
        /// 异步创建代理，并初始化其租户数据库和基础数据。
        /// </summary>
        private async Task CreateAsync()
        {
            if (!IsNew) throw new InvalidOperationException("请先点击“新增代理”。");
            if (string.IsNullOrWhiteSpace(AgentName)) throw new InvalidOperationException("请输入代理名称。" );
            var request = BuildAgent();
            var result = await Task.Run(() => provisioning.Create(request));
            await RefreshAsync();
            SelectedAgent = Agents.FirstOrDefault(x => x.AgentID == result.AgentID);
            InitialCredentials = "业务登录账号：admin    默认密码：" + result.InitialPassword;
            await WriteLogAsync("创建代理及分库", $"代理：{request.AgentName}；代理号：{result.AgentID:D}。已创建分库、专用 SQL 账号及业务管理员。");
            Notice = "代理库、专用 SQL 账号和业务管理员已创建。";
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            if (IsNew) throw new InvalidOperationException("请先选择代理。" );
            var update = BuildAgent();
            update.AgentID = SelectedAgent.AgentID;
            if (string.IsNullOrWhiteSpace(update.AgentName)) throw new InvalidOperationException("请输入代理名称。" );
            await Task.Run(() => agents.UpdateDetails(update));
            await WriteLogAsync("保存代理资料", $"代理：{update.AgentName}；代理号：{update.AgentID:D}。");
            await RefreshAsync();
            Notice = "代理资料已保存。";
        }

        /// <summary>
        /// 异步重试当前代理的租户初始化流程。
        /// </summary>
        private async Task RetryAsync()
        {
            if (IsNew) throw new InvalidOperationException("请先选择代理。" );
            var result = await Task.Run(() => provisioning.Retry(SelectedAgent.AgentID));
            await RefreshAsync();
            InitialCredentials = "业务登录账号：admin    默认密码：" + result.InitialPassword;
            await WriteLogAsync("重试开通代理", $"代理：{SelectedAgent.AgentName}；代理号：{result.AgentID:D}。开通已完成。");
            Notice = "代理开通已完成。";
        }

        /// <summary>
        /// 异步切换当前代理的启用状态。
        /// </summary>
        private async Task ToggleActiveAsync()
        {
            if (IsNew) throw new InvalidOperationException("请先选择代理。" );
            var active = SelectedAgent.State != 1;
            await Task.Run(() => provisioning.SetActive(SelectedAgent.AgentID, active));
            await WriteLogAsync(active ? "启用代理" : "停用代理",
                $"代理：{SelectedAgent.AgentName}；代理号：{SelectedAgent.AgentID:D}。");
            await RefreshAsync();
            Notice = active ? "代理已启用。" : "代理已停用，新的 SQL 连接将被拒绝。";
        }

        /// <summary>
        /// 根据编辑区内容构造代理实体。
        /// </summary>
        private Agent BuildAgent() => new Agent
        {
            AgentName = AgentName?.Trim(), Contact = Contact?.Trim(), Tel = Tel?.Trim(),
            AgentAddress = Address?.Trim(), AgentProvince = Province?.Trim(), AgentCity = City?.Trim()
        };

        /// <summary>
        /// 异步写入主库管理审计日志；日志失败不影响已成功完成的业务操作。
        /// </summary>
        private async Task WriteLogAsync(string eventName, string info)
        {
            try
            {
                await Task.Run(() => adminLogs.Write(session.DisplayName, eventName, info));
            }
            catch
            {
                // 审计表不可用时不回滚已经成功完成的代理操作。
            }
        }

        /// <summary>
        /// 清空编辑区并重置表单状态。
        /// </summary>
        private void ClearEditor()
        {
            SelectedAgent = null;
            AgentName = Contact = Tel = Address = Province = City = null;
            InitialCredentials = null;
            Notice = "填写代理资料后点击“创建代理及分库”。";
        }

        /// <summary>取消代理资料编辑，并从已选代理恢复尚未保存的字段。</summary>
        private void CancelAgentEditor()
        {
            if (SelectedAgent == null)
            {
                ClearEditor();
                return;
            }
            AgentName = SelectedAgent.AgentName;
            Contact = SelectedAgent.Contact;
            Tel = SelectedAgent.Tel;
            Address = SelectedAgent.AgentAddress;
            Province = SelectedAgent.AgentProvince;
            City = SelectedAgent.AgentCity;
        }

        /// <summary>
        /// 统一执行管理操作，维护忙碌状态并处理操作异常。
        /// </summary>
        private async Task RunAsync(Func<Task> action)
        {
            if (IsBusy) return;
            IsBusy = true;
            try { await action(); }
            catch (Exception ex) { Notice = ex.Message; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 设置管理窗口的显示状态。
        /// </summary>
        private static void SetWindowState(WindowState state)
        {
            if (Application.Current.MainWindow != null) Application.Current.MainWindow.WindowState = state;
        }
    }
}
