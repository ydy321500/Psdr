using Prism.Mvvm;
using Psdr.Dals.Configuration;
using System.Collections.Generic;

namespace Psdr.Desktop.ViewModels.Common
{
        /// <summary>Shared editable province and city selection for customer and supplier forms.</summary>
        public sealed class ProvinceCitySelection : BindableBase
        {
        private readonly ProvinceCityCatalog catalog;
        private string province;
        private string city;
        private IReadOnlyList<string> cityOptions;
        
        /// <summary>
        /// 初始化。
        /// </summary>
        public ProvinceCitySelection(ProvinceCityCatalog catalog)
        {
            this.catalog = catalog;
            cityOptions = catalog.GetCities(null);
        }

        public IReadOnlyList<string> Provinces => catalog.Provinces;
        public IReadOnlyList<string> Cities { get => cityOptions; private set => SetProperty(ref cityOptions, value); }
        public string Province
        {
            get => province;
            set
            {
                if (!SetProperty(ref province, value)) return;
                Cities = catalog.GetCities(value);
                City = string.Empty;
            }
        }
        public string City { get => city; set => SetProperty(ref city, value); }

        /// <summary>
        /// 设置当前选择的省份和城市值。
        /// </summary>
        public void SetValues(string provinceValue, string cityValue)
        {
            Province = provinceValue;
            City = cityValue;
        }
    }
}
