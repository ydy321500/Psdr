using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Dictionary;
using Psdr.Dals.ProductMap;
using Psdr.Desktop.Services;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.Dictionary
{
    public sealed class DictionaryEntry
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public DictionaryEntry(DictionaryTable data, string displayName)
        {
            Data = data;
            DisplayName = displayName;
        }

        public DictionaryTable Data { get; }
        public string DisplayName { get; }
    }

    public sealed class DictionaryViewModel : BindableBase
    {
        private readonly DictionaryDAL dal;
        private readonly ProductMapDAL productsDal;
        private readonly IConfirmationService confirmation;
        private ObservableCollection<TenantFunction> products = new ObservableCollection<TenantFunction>();
        private ObservableCollection<DictionaryEntry> entries = new ObservableCollection<DictionaryEntry>();
        private ObservableCollection<Psdr.Models.Entities.ProductMap> attributes =
            new ObservableCollection<Psdr.Models.Entities.ProductMap>();
        private TenantFunction selectedProduct;
        private DictionaryEntry selectedEntry;
        private Psdr.Models.Entities.ProductMap selectedAttribute;
        private string editorModel = "产品";
        private string editorBusinessName;
        private string editorValue;
        private string statusMessage = "正在读取字典…";
        private bool isBusy;
        private bool isEditing;
        private int editingId;

        /// <summary>
        /// 初始化字典视图模型及其数据加载、编辑命令。
        /// </summary>
        public DictionaryViewModel(DictionaryDAL dal, ProductMapDAL productsDal,
            IConfirmationService confirmation)
        {
            this.dal = dal;
            this.productsDal = productsDal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(StartNew, () => !IsBusy && SelectedProduct != null);
            EditCommand = new DelegateCommand<DictionaryEntry>(item =>
            {
                SelectedEntry = item;
                StartEdit();
            }, item => !IsBusy && item != null && SelectedProduct != null);
            DeleteCommand = new DelegateCommand<DictionaryEntry>(async item =>
            {
                SelectedEntry = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null && SelectedProduct != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<TenantFunction> Products
        { get => products; private set => SetProperty(ref products, value); }
        public ObservableCollection<DictionaryEntry> Entries
        {
            get => entries;
            private set
            {
                if (!SetProperty(ref entries, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
        public ObservableCollection<Psdr.Models.Entities.ProductMap> Attributes
        { get => attributes; private set => SetProperty(ref attributes, value); }
        public TenantFunction SelectedProduct
        {
            get => selectedProduct;
            set
            {
                if (!SetProperty(ref selectedProduct, value)) return;
                IsEditing = false;
                NewCommand.RaiseCanExecuteChanged();
                _ = LoadEntriesAsync();
            }
        }
        public DictionaryEntry SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (!SetProperty(ref selectedEntry, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }
        public Psdr.Models.Entities.ProductMap SelectedAttribute
        { get => selectedAttribute; set => SetProperty(ref selectedAttribute, value); }
        public string EditorModel
        {
            get => editorModel;
            set
            {
                if (!SetProperty(ref editorModel, value)) return;
                RaisePropertyChanged(nameof(IsProductModel));
                RaisePropertyChanged(nameof(IsBusinessModel));
            }
        }
        public bool IsProductModel => EditorModel == "产品";
        public bool IsBusinessModel => EditorModel == "业务";
        public string EditorBusinessName
        { get => editorBusinessName; set => SetProperty(ref editorBusinessName, value); }
        public string EditorValue { get => editorValue; set => SetProperty(ref editorValue, value); }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsEditing
        { get => isEditing; private set { if (SetProperty(ref isEditing, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged();
                NewCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand<DictionaryEntry> EditCommand { get; }
        public DelegateCommand<DictionaryEntry> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        /// <summary>
        /// 加载字典数据。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        /// <summary>
        /// 异步刷新管理页面的数据和状态。
        /// </summary>
        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var previous = SelectedProduct?.FunctionName;
                var list = await Task.Run(() => productsDal.GetActiveProducts());
                Products = new ObservableCollection<TenantFunction>(list);
                SelectedProduct = list.FirstOrDefault(x => x.FunctionName == previous) ?? list.FirstOrDefault();
                if (SelectedProduct == null)
                {
                    Entries.Clear();
                    Attributes.Clear();
                    StatusMessage = "当前代理没有已启用且未过期的产品类型。";
                }
            }
            catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 异步加载字典条目。
        /// </summary>
        private async Task LoadEntriesAsync()
        {
            var product = SelectedProduct;
            if (product == null) return;
            try
            {
                var result = await Task.Run(() => new
                {
                    Attributes = dal.GetEnabledAttributes(product.FunctionName),
                    Entries = dal.GetAll(product.FunctionName)
                });
                if (SelectedProduct != product) return;
                Attributes = new ObservableCollection<Psdr.Models.Entities.ProductMap>(result.Attributes);
                Entries = new ObservableCollection<DictionaryEntry>(result.Entries.Select(entry =>
                    new DictionaryEntry(entry, entry.DicModel == "产品"
                        ? result.Attributes.FirstOrDefault(x => x.KeyName == entry.DicName)?.ValueName
                            ?? entry.DicName : entry.DicName)));
                StatusMessage = $"{product.FunctionName}：共 {Entries.Count} 条字典。";
            }
            catch (Exception ex) { if (SelectedProduct == product) StatusMessage = "读取字典失败：" + ex.Message; }
        }

        /// <summary>
        /// 初始化新建数据的编辑状态。
        /// </summary>
        private void StartNew()
        {
            editingId = 0;
            EditorModel = "产品";
            SelectedAttribute = Attributes.FirstOrDefault();
            EditorBusinessName = string.Empty;
            EditorValue = string.Empty;
            IsEditing = true;
        }

        /// <summary>
        /// 将选中数据加载到编辑区。
        /// </summary>
        private void StartEdit()
        {
            var item = SelectedEntry?.Data;
            if (item == null) return;
            editingId = item.DicID;
            EditorModel = item.DicModel;
            SelectedAttribute = Attributes.FirstOrDefault(x => x.KeyName == item.DicName);
            EditorBusinessName = item.DicModel == "业务" ? item.DicName : string.Empty;
            EditorValue = item.DicValue;
            IsEditing = true;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            var product = SelectedProduct;
            if (IsBusy || !IsEditing || product == null) return;
            var item = new DictionaryTable
            {
                DicID = editingId, ProductName = product.FunctionName, DicModel = EditorModel,
                DicName = IsProductModel ? SelectedAttribute?.KeyName : EditorBusinessName,
                DicValue = EditorValue
            };
            IsBusy = true;
            try
            {
                var saved = await Task.Run(() => dal.Save(item));
                if (!saved) { StatusMessage = "保存失败：记录已不存在。"; return; }
                IsEditing = false;
                await LoadEntriesAsync();
                StatusMessage = "字典已保存。";
            }
            catch (Exception ex) { StatusMessage = "保存失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        /// <summary>
        /// 异步删除当前选中的数据记录。
        /// </summary>
        private async Task DeleteAsync()
        {
            var selected = SelectedEntry;
            var product = SelectedProduct;
            if (IsBusy || selected == null || product == null) return;
            if (!confirmation.Confirm($"确认删除字典“{selected.DisplayName}：{selected.Data.DicValue}”？")) return;
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Delete(selected.Data.DicID, product.FunctionName)))
                { StatusMessage = "删除失败：记录已不存在。"; return; }
                await LoadEntriesAsync();
                StatusMessage = "字典已删除。";
            }
            catch (Exception ex) { StatusMessage = "删除失败：" + ex.Message; }
            finally { IsBusy = false; }
        }
    }
}
