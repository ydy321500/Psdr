using Prism.Commands;
using Prism.Mvvm;
using Psdr.Dals.ProductMap;
using Psdr.Models.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Psdr.Desktop.ViewModels.Set.ProductMap
{
    public sealed class ProductMapViewModel : BindableBase
    {
        private readonly ProductMapDAL dal;
        private ObservableCollection<TenantFunction> products = new ObservableCollection<TenantFunction>();
        private ObservableCollection<Psdr.Models.Entities.ProductMap> maps =
            new ObservableCollection<Psdr.Models.Entities.ProductMap>();
        private TenantFunction selectedProduct;
        private Psdr.Models.Entities.ProductMap selectedMap;
        private string editorName;
        private string editorType;
        private bool editorEnabled;
        private string statusMessage = "正在读取产品类型…";
        private bool isBusy;
        private bool isEditing;

        /// <summary>
        /// 初始化产品属性视图模型及其编辑命令。
        /// </summary>
        public ProductMapViewModel(ProductMapDAL dal)
        {
            this.dal = dal;
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync(), () => !IsBusy);
            EditCommand = new DelegateCommand<Psdr.Models.Entities.ProductMap>(map =>
            {
                SelectedMap = map;
                StartEdit();
            }, map => !IsBusy && map != null);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && IsEditing);
            CancelCommand = new DelegateCommand(() => IsEditing = false);
        }

        public ObservableCollection<TenantFunction> Products
        {
            get => products;
            private set => SetProperty(ref products, value);
        }

        public ObservableCollection<Psdr.Models.Entities.ProductMap> Maps
        {
            get => maps;
            private set
            {
                if (SetProperty(ref maps, value)) EditCommand.RaiseCanExecuteChanged();
            }
        }

        public TenantFunction SelectedProduct
        {
            get => selectedProduct;
            set
            {
                if (!SetProperty(ref selectedProduct, value)) return;
                IsEditing = false;
                _ = LoadMapsAsync();
            }
        }

        public Psdr.Models.Entities.ProductMap SelectedMap
        {
            get => selectedMap;
            set
            {
                if (SetProperty(ref selectedMap, value)) EditCommand.RaiseCanExecuteChanged();
            }
        }

        public string EditorKey => SelectedMap?.KeyName ?? string.Empty;
        public string EditorName { get => editorName; set => SetProperty(ref editorName, value); }
        public string EditorType { get => editorType; set => SetProperty(ref editorType, value); }
        public bool EditorEnabled { get => editorEnabled; set => SetProperty(ref editorEnabled, value); }
        public string StatusMessage { get => statusMessage; private set => SetProperty(ref statusMessage, value); }
        public bool IsEditing
        {
            get => isEditing;
            private set
            {
                if (SetProperty(ref isEditing, value)) SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (!SetProperty(ref isBusy, value)) return;
                RefreshCommand.RaiseCanExecuteChanged();
                EditCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand<Psdr.Models.Entities.ProductMap> EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 加载产品属性数据。
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
                var oldName = SelectedProduct?.FunctionName;
                var list = await Task.Run(() => dal.GetActiveProducts());
                Products = new ObservableCollection<TenantFunction>(list);
                SelectedProduct = list.FirstOrDefault(x => x.FunctionName == oldName) ?? list.FirstOrDefault();
                if (SelectedProduct == null)
                {
                    Maps = new ObservableCollection<Psdr.Models.Entities.ProductMap>();
                    StatusMessage = "当前代理没有已启用且未过期的产品类型。";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "读取失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 异步加载产品属性映射数据。
        /// </summary>
        private async Task LoadMapsAsync()
        {
            var product = SelectedProduct;
            if (product == null) return;
            try
            {
                var list = await Task.Run(() => dal.GetMaps(product.FunctionName));
                if (SelectedProduct != product) return;
                Maps = new ObservableCollection<Psdr.Models.Entities.ProductMap>(list);
                StatusMessage = $"{product.FunctionName}：共 {list.Count} 个属性位置。";
            }
            catch (Exception ex)
            {
                if (SelectedProduct == product) StatusMessage = "读取属性失败：" + ex.Message;
            }
        }

        /// <summary>
        /// 将选中数据加载到编辑区。
        /// </summary>
        private void StartEdit()
        {
            if (SelectedMap == null) return;
            EditorName = SelectedMap.ValueName;
            EditorType = SelectedMap.FieldType == "选填" ? "选填" : "必填";
            EditorEnabled = SelectedMap.State == 1;
            RaisePropertyChanged(nameof(EditorKey));
            IsEditing = true;
        }

        /// <summary>
        /// 异步保存当前编辑的数据。
        /// </summary>
        private async Task SaveAsync()
        {
            var product = SelectedProduct;
            var map = SelectedMap;
            if (IsBusy || !IsEditing || product == null || map == null) return;
            IsBusy = true;
            try
            {
                var saved = await Task.Run(() => dal.Save(map.MapID, product.FunctionName,
                    EditorName, EditorType, EditorEnabled));
                if (!saved)
                {
                    StatusMessage = "保存失败：该属性已不存在。";
                    return;
                }
                IsEditing = false;
                await LoadMapsAsync();
                StatusMessage = "产品属性已保存。";
            }
            catch (Exception ex)
            {
                StatusMessage = "保存失败：" + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
