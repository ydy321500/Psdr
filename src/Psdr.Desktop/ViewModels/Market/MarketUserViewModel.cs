using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Market;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Market
{
    /// <summary>
    /// 维护市场端会员账号及其联系方式、收货地址和启停状态。
    /// </summary>
    public sealed class MarketUserViewModel : BindableBase
    {
        private readonly MarketUserDAL dal;
        private M_User selectedItem;
        private string userCode;
        private string userName;
        private string password;
        private string tel;
        private string address;
        private bool enabled = true;
        private string status = "正在读取市场会员…";
        private bool isBusy;

        /// <summary>
        /// 初始化市场会员管理所需的数据服务和命令。
        /// </summary>
        public MarketUserViewModel(MarketUserDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(New, () => !IsBusy);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
        }

        public ObservableCollection<M_User> Items { get; } = new ObservableCollection<M_User>();

        public M_User SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!SetProperty(ref selectedItem, value)) return;
                UserCode = value?.UserCode;
                UserName = value?.UserName;
                Password = value?.UserPsw;
                Tel = value?.Tel;
                Address = value?.Address;
                Enabled = value == null || value.State == 0;
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string UserCode { get => userCode; set { if (SetProperty(ref userCode, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public string UserName { get => userName; set { if (SetProperty(ref userName, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public string Password { get => password; set { if (SetProperty(ref password, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public string Tel { get => tel; set => SetProperty(ref tel, value); }
        public string Address { get => address; set => SetProperty(ref address, value); }
        public bool Enabled { get => enabled; set => SetProperty(ref enabled, value); }
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public bool IsBusy { get => isBusy; private set { if (SetProperty(ref isBusy, value)) { RefreshCommand.RaiseCanExecuteChanged(); NewCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 页面打开时读取市场会员。
        /// </summary>
        public void Load()
        {
            _ = RefreshAsync();
        }

        private bool CanSave()
        {
            return !IsBusy && !string.IsNullOrWhiteSpace(UserCode) && !string.IsNullOrWhiteSpace(UserName) &&
                !string.IsNullOrWhiteSpace(Password);
        }

        private void New()
        {
            SelectedItem = null;
            UserCode = UserName = Password = Tel = Address = string.Empty;
            Enabled = true;
            Status = "填写市场会员资料后保存。";
        }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var currentId = SelectedItem?.UserID;
                var users = await Task.Run(() => dal.GetAll());
                Items.Clear();
                foreach (var user in users) Items.Add(user);
                SelectedItem = currentId.HasValue ? Items.FirstOrDefault(x => x.UserID == currentId.Value) : null;
                Status = $"已读取 {Items.Count} 个市场会员。";
            }
            catch (Exception ex)
            {
                Status = "读取市场会员失败：" + ex.Message;
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
                var item = new M_User
                {
                    UserID = SelectedItem?.UserID ?? 0,
                    UserCode = UserCode,
                    UserName = UserName,
                    UserPsw = Password,
                    Tel = Tel,
                    Address = Address,
                    State = Enabled ? 0 : 1
                };
                await Task.Run(() => dal.Save(item));
                Status = "市场会员已保存。";
                IsBusy = false;
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                Status = "保存市场会员失败：" + ex.Message;
            }
            finally
            {
                if (IsBusy) IsBusy = false;
            }
        }
    }
}
