using Prism.Commands; using Prism.Mvvm; using Psdr.Dals.Set; using Psdr.Models.Entities; using System; using System.Collections.ObjectModel; using System.Threading.Tasks;
namespace Psdr.Desktop.ViewModels.Set
{
    /// <summary>展示代理库操作日志。</summary>
    public sealed class LogViewModel : BindableBase
    {
        private readonly LogDAL dal; private string status="正在读取日志…"; private bool busy;
        /// <summary>初始化日志页面。</summary>
        public LogViewModel(LogDAL dal){this.dal=dal;RefreshCommand=new DelegateCommand(async()=>await RefreshAsync(),()=>!Busy);}
        public ObservableCollection<Log> Items{get;}=new ObservableCollection<Log>(); public string Status{get=>status;private set=>SetProperty(ref status,value);} public bool Busy{get=>busy;private set{if(SetProperty(ref busy,value))RefreshCommand.RaiseCanExecuteChanged();}} public DelegateCommand RefreshCommand{get;}
        /// <summary>加载日志。</summary>
        public void Load(){_=RefreshAsync();}
        private async Task RefreshAsync(){if(Busy)return;Busy=true;try{var x=await Task.Run(()=>dal.GetRecent(500));Items.Clear();foreach(var i in x)Items.Add(i);Status=$"最近 {Items.Count} 条日志。";}catch(Exception ex){Status="读取失败："+ex.Message;}finally{Busy=false;}}
    }
}
