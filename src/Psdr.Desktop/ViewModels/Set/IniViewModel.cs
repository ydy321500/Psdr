using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Set;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Set
{
    /// <summary>维护代理库系统配置项。</summary>
    public sealed class IniViewModel : BindableBase
    {
        private readonly IniDAL dal; private Ini selected; private string status = "正在读取配置…"; private bool busy;
        /// <summary>初始化系统设置页面。</summary>
        public IniViewModel(IniDAL dal) { this.dal = dal; RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !Busy); NewCommand = new DelegateCommand(New); SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !Busy && Selected != null); }
        public ObservableCollection<Ini> Items { get; } = new ObservableCollection<Ini>();
        public Ini Selected { get => selected; set { if (SetProperty(ref selected, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public string Status { get => status; private set => SetProperty(ref status, value); }
        public bool Busy { get => busy; private set { if (SetProperty(ref busy, value)) { RefreshCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged(); } } }
        public DelegateCommand RefreshCommand { get; } public DelegateCommand NewCommand { get; } public DelegateCommand SaveCommand { get; }
        /// <summary>加载配置项。</summary>
        public void Load() { _ = RefreshAsync(); }
        private async Task RefreshAsync() { if (Busy) return; Busy = true; try { var x = await Task.Run(() => dal.GetAll()); Items.Clear(); foreach (var i in x) Items.Add(i); Status = $"共 {Items.Count} 个配置项。"; } catch (Exception ex) { Status = "读取失败：" + ex.Message; } finally { Busy = false; } }
        private void New() { Selected = new Ini { Type = 0, State = 1 }; Items.Add(Selected); }
        private async Task SaveAsync() { if (Selected == null || string.IsNullOrWhiteSpace(Selected.Title)) { Status = "配置名称不能为空。"; return; } Busy = true; try { if (!await Task.Run(() => dal.Save(Selected))) throw new InvalidOperationException("配置未保存。"); Status = "配置已保存。"; } catch (Exception ex) { Status = "保存失败：" + ex.Message; } finally { Busy = false; } }
    }
}
