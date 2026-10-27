using Prism.Commands; using Prism.Mvvm; using Psdr.Dals.Market; using Psdr.Dals.Product; using Psdr.Desktop.Services; using Psdr.Models.Entities; using System; using System.Collections.ObjectModel; using System.Threading.Tasks;
namespace Psdr.Desktop.ViewModels.Market
{
    /// <summary>维护市场展示产品。</summary>
    public sealed class MarketProductViewModel : BindableBase
    {
        private readonly MarketProductDAL dal; private readonly ProductDAL productDal; private readonly IConfirmationService confirmation; private M_Product selected; private Product product; private string status="正在加载市场产品…"; private bool busy;
        /// <summary>初始化市场产品页面。</summary>
        public MarketProductViewModel(MarketProductDAL dal, ProductDAL productDal, IConfirmationService confirmation) { this.dal=dal; this.productDal=productDal; this.confirmation=confirmation; RefreshCommand=new DelegateCommand(async()=>await RefreshAsync(),()=>!Busy); NewCommand=new DelegateCommand(New); SaveCommand=new DelegateCommand(async()=>await SaveAsync(),()=>!Busy&&Selected!=null); DeleteCommand=new DelegateCommand(async()=>await DeleteAsync(),()=>!Busy&&Selected?.ID>0); CancelCommand=new DelegateCommand(async()=>await CancelAsync()); }
        public ObservableCollection<M_Product> Items { get; }=new ObservableCollection<M_Product>(); public ObservableCollection<Product> Products { get; }=new ObservableCollection<Product>();
        public M_Product Selected { get=>selected; set { if(SetProperty(ref selected,value)){ Product=Products.Count==0?null:Find(value?.ProductID??0); SaveCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); } } }
        public Product Product { get=>product; set { if(SetProperty(ref product,value)&&value!=null&&Selected!=null) { Selected.ProductID=value.ProductID; Selected.ProductName=value.ProductName; } } }
        public string Status { get=>status; private set=>SetProperty(ref status,value); } public bool Busy { get=>busy; private set { if(SetProperty(ref busy,value)){RefreshCommand.RaiseCanExecuteChanged();SaveCommand.RaiseCanExecuteChanged();DeleteCommand.RaiseCanExecuteChanged();} } }
        public DelegateCommand RefreshCommand{get;} public DelegateCommand NewCommand{get;} public DelegateCommand SaveCommand{get;} public DelegateCommand DeleteCommand{get;} public DelegateCommand CancelCommand{get;}
        /// <summary>加载市场和本地产品。</summary>
        public void Load(){_=RefreshAsync();}
        private async Task RefreshAsync(){if(Busy)return;Busy=true;try{var x=await Task.Run(()=>new{M=dal.GetAll(),P=productDal.GetAll()});Items.Clear();foreach(var i in x.M)Items.Add(i);Products.Clear();foreach(var p in x.P)Products.Add(p);Status=$"共 {Items.Count} 个市场产品。";}catch(Exception ex){Status="读取失败："+ex.Message;}finally{Busy=false;}}
        private void New(){Selected=new M_Product{ProductType=0};Items.Add(Selected);}
        /// <summary>关闭编辑窗时移除未保存的市场产品草稿，并恢复数据库中的列表。</summary>
        private async Task CancelAsync(){Selected=null;await RefreshAsync();}
        private async Task SaveAsync(){try{if(Product==null)throw new InvalidOperationException("请选择本地产品。");Busy=true;var id=await Task.Run(()=>dal.Save(Selected));Status="市场产品已保存。";await RefreshAsync();Selected=FindMarketProduct(id);}catch(Exception ex){Status="保存失败："+ex.Message;}finally{if(Busy)Busy=false;}}
        private async Task DeleteAsync(){if(Selected==null||!confirmation.Confirm($"确认删除市场产品“{Selected.ProductName}”？这不会删除本地产品资料。"))return;try{Busy=true;if(!await Task.Run(()=>dal.Delete(Selected.ID)))throw new InvalidOperationException("删除失败。");Items.Remove(Selected);Selected=null;Status="市场产品已删除。";}catch(Exception ex){Status="删除失败："+ex.Message;}finally{Busy=false;}}
        private Product Find(int id){foreach(var p in Products)if(p.ProductID==id)return p;return null;}
        private M_Product FindMarketProduct(int id){foreach(var item in Items)if(item.ID==id)return item;return null;}
    }
}
