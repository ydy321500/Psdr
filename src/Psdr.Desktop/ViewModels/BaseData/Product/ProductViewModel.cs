using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.Product;
using Psdr.Desktop.Services;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.BaseData.Product
{
    /// <summary>
    /// 表示产品编辑区中的一个动态属性值。
    /// </summary>
    public sealed class ProductFieldEditor : BindableBase
    {
        private string value;

        /// <summary>
        /// 创建产品动态属性编辑项。
        /// </summary>
        public ProductFieldEditor(string key, string label, bool required, string value)
        {
            Key = key;
            Label = label;
            IsRequired = required;
            this.value = value;
        }

        public string Key { get; }
        public string Label { get; }
        public bool IsRequired { get; }
        public string Value { get => value; set => SetProperty(ref this.value, value); }
    }

    /// <summary>
    /// 管理产品资料列表、产品类型和动态属性编辑状态。
    /// </summary>
    public sealed class ProductViewModel : BindableBase
    {
        private readonly ProductDAL dal;
        private readonly IConfirmationService confirmation;
        private List<Psdr.Models.Entities.Product> all = new List<Psdr.Models.Entities.Product>();
        private ObservableCollection<Psdr.Models.Entities.Product> items =
            new ObservableCollection<Psdr.Models.Entities.Product>();
        private ObservableCollection<TenantFunction> productTypes = new ObservableCollection<TenantFunction>();
        private ObservableCollection<ProductFieldEditor> fields = new ObservableCollection<ProductFieldEditor>();
        private Psdr.Models.Entities.Product selectedItem;
        private TenantFunction selectedType;
        private string searchText;
        private string statusMessage = "正在准备产品资料…";
        private decimal? purchasePrice;
        private decimal? salePrice;
        private decimal? redPacketPriceLow;
        private decimal? redPacketPriceHigh;
        private string position;
        private float? volume;
        private bool isEditing;
        private bool isBusy;
        private int editingId;

        /// <summary>
        /// 初始化产品资料页面所需的数据访问服务和界面命令。
        /// </summary>
        public ProductViewModel(ProductDAL dal, IConfirmationService confirmation)
        {
            this.dal = dal;
            this.confirmation = confirmation;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new DelegateCommand(async () => await StartNewAsync(), () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.Product>(async item =>
            {
                SelectedItem = item;
                await StartEditAsync();
            }, item => !IsBusy && item != null);
            DeleteCommand = new DelegateCommand<Psdr.Models.Entities.Product>(async item =>
            {
                SelectedItem = item;
                await DeleteAsync();
            }, item => !IsBusy && item != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<Psdr.Models.Entities.Product> Items
        {
            get => items;
            private set
            {
                if (!SetProperty(ref items, value)) return;
                EditCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<TenantFunction> ProductTypes { get => productTypes; private set => SetProperty(ref productTypes, value); }
        public ObservableCollection<ProductFieldEditor> Fields { get => fields; private set => SetProperty(ref fields, value); }
        public Psdr.Models.Entities.Product SelectedItem { get => selectedItem; set => SetProperty(ref selectedItem, value); }
        public TenantFunction SelectedType
        {
            get => selectedType;
            set
            {
                if (!SetProperty(ref selectedType, value) || !IsEditing || editingId != 0) return;
                _ = LoadFieldsAsync(null);
            }
        }
        public string SearchText { get => searchText; set { if (SetProperty(ref searchText, value)) ApplyFilter(); } }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public string EditorTitle => editingId == 0 ? "新增产品" : "编辑产品";
        public bool CanChangeType => editingId == 0;
        public decimal? PurchasePrice { get => purchasePrice; set => SetProperty(ref purchasePrice, value); }
        public decimal? SalePrice { get => salePrice; set => SetProperty(ref salePrice, value); }
        public decimal? RedPacketPriceLow { get => redPacketPriceLow; set => SetProperty(ref redPacketPriceLow, value); }
        public decimal? RedPacketPriceHigh { get => redPacketPriceHigh; set => SetProperty(ref redPacketPriceHigh, value); }
        public string Position { get => position; set => SetProperty(ref position, value); }
        public float? Volume { get => volume; set => SetProperty(ref volume, value); }
        public bool IsEditing { get => isEditing; private set { if (SetProperty(ref isEditing, value)) SaveCommand.RaiseCanExecuteChanged(); } }
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged(); NewCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged(); DeleteCommand.RaiseCanExecuteChanged(); SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NewCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.Product> EditCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.Product> DeleteCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 加载产品类型及产品资料。
        /// </summary>
        public void Load() { _ = RefreshAsync(); }

        private async Task RefreshAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var result = await Task.Run(() => new { Types = dal.GetActiveTypes(), Products = dal.GetAll() });
                ProductTypes = new ObservableCollection<TenantFunction>(result.Types);
                all = result.Products;
                ApplyFilter();
                StatusMessage = $"共 {all.Count} 个产品，可用产品类型 {ProductTypes.Count} 个。";
            }
            catch (Exception ex) { StatusMessage = "读取失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private void ApplyFilter()
        {
            var keyword = (SearchText ?? string.Empty).Trim();
            Items = new ObservableCollection<Psdr.Models.Entities.Product>(string.IsNullOrEmpty(keyword) ? all :
                all.Where(x => Contains(x.ProductName, keyword) || Contains(x.K1, keyword) || Contains(x.K2, keyword) || Contains(x.Position, keyword)));
        }

        private async Task StartNewAsync()
        {
            editingId = 0;
            SelectedType = ProductTypes.FirstOrDefault();
            PurchasePrice = SalePrice = RedPacketPriceLow = RedPacketPriceHigh = null;
            Position = string.Empty;
            Volume = null;
            await LoadFieldsAsync(null);
            RaisePropertyChanged(nameof(EditorTitle));
            RaisePropertyChanged(nameof(CanChangeType));
            IsEditing = true;
        }

        private async Task StartEditAsync()
        {
            var item = SelectedItem;
            if (item == null) return;
            editingId = item.ProductID;
            SelectedType = ProductTypes.FirstOrDefault(x => x.FunctionName == item.ProductName);
            PurchasePrice = item.PurchasePrice;
            SalePrice = item.Price;
            RedPacketPriceLow = item.RedPacketPriceLow;
            RedPacketPriceHigh = item.RedPacketPriceHigh;
            Position = item.Position;
            Volume = item.Volume;
            await LoadFieldsAsync(item);
            RaisePropertyChanged(nameof(EditorTitle));
            RaisePropertyChanged(nameof(CanChangeType));
            IsEditing = true;
        }

        private async Task LoadFieldsAsync(Psdr.Models.Entities.Product item)
        {
            var type = SelectedType;
            if (type == null) { Fields = new ObservableCollection<ProductFieldEditor>(); return; }
            try
            {
                var maps = await Task.Run(() => dal.GetEnabledFields(type.FunctionName));
                if (SelectedType != type) return;
                Fields = new ObservableCollection<ProductFieldEditor>(maps.Select(map => new ProductFieldEditor(
                    map.KeyName, map.ValueName, map.FieldType == "必填", GetFieldValue(item, map.KeyName))));
            }
            catch (Exception ex) { StatusMessage = "读取产品属性失败：" + ex.Message; }
        }

        private async Task SaveAsync()
        {
            if (IsBusy || !IsEditing || SelectedType == null) { StatusMessage = "请选择产品类型。"; return; }
            if (Fields.Any(x => x.IsRequired && string.IsNullOrWhiteSpace(x.Value)))
            { StatusMessage = "请填写所有必填产品属性。"; return; }
            if (PurchasePrice < 0 || SalePrice < 0 || RedPacketPriceLow < 0 || RedPacketPriceHigh < 0 || Volume < 0)
            { StatusMessage = "价格和体积不能小于零。"; return; }

            var item = new Psdr.Models.Entities.Product
            {
                ProductID = editingId, ProductName = SelectedType.FunctionName,
                PurchasePrice = PurchasePrice, Price = SalePrice, RedPacketPriceLow = RedPacketPriceLow,
                RedPacketPriceHigh = RedPacketPriceHigh, Position = (Position ?? string.Empty).Trim(), Volume = Volume
            };
            foreach (var field in Fields) SetFieldValue(item, field.Key, field.Value);

            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Save(item))) { StatusMessage = "保存失败：产品记录可能已被删除。"; return; }
                IsEditing = false;
                all = await Task.Run(() => dal.GetAll());
                ApplyFilter();
                StatusMessage = "产品已保存。";
            }
            catch (Exception ex) { StatusMessage = "保存失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task DeleteAsync()
        {
            var item = SelectedItem;
            if (IsBusy || item == null || !confirmation.Confirm($"确认删除产品编号 {item.ProductID}？")) return;
            IsBusy = true;
            try
            {
                if (!await Task.Run(() => dal.Delete(item.ProductID))) { StatusMessage = "删除失败：记录可能已被删除。"; return; }
                all = await Task.Run(() => dal.GetAll());
                ApplyFilter();
                StatusMessage = "产品已删除。";
            }
            catch (Exception ex) { StatusMessage = "删除失败：" + ex.Message; }
            finally { IsBusy = false; }
        }

        private static bool Contains(string value, string keyword) => (value ?? string.Empty).IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0;
        private static string GetFieldValue(Psdr.Models.Entities.Product item, string key) => item == null ? string.Empty : (string)typeof(Psdr.Models.Entities.Product).GetProperty(key)?.GetValue(item) ?? string.Empty;
        private static void SetFieldValue(Psdr.Models.Entities.Product item, string key, string value)
        {
            var property = typeof(Psdr.Models.Entities.Product).GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.PropertyType == typeof(string)) property.SetValue(item, (value ?? string.Empty).Trim());
        }
    }
}
