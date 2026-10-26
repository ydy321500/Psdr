using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Psdr.Dals.Configuration
{
        /// <summary>Reads the province and city list shipped with the newer PSDR client.</summary>
        public sealed class ProvinceCityCatalog
        {
        private readonly Dictionary<string, IReadOnlyList<string>> cities =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> provinces = new List<string>();
        
        /// <summary>
            /// 从嵌入资源加载省份及其可选城市列表。
        /// </summary>
        public ProvinceCityCatalog()
        {
            const string resourceName = "Psdr.Dals.Data.pc.txt";
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) throw new InvalidOperationException("省市数据资源未包含在程序中。");
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var separator = line.IndexOf('=');
                        if (separator <= 0) continue;
                        var province = line.Substring(0, separator).Trim();
                        var values = line.Substring(separator + 1).Split(',');
                        var cityList = new List<string>();
                        foreach (var value in values)
                        {
                            var city = value.Trim();
                            if (city.Length > 0) cityList.Add(city);
                        }
                        if (province.Length == 0 || cities.ContainsKey(province)) continue;
                        provinces.Add(province);
                        cities.Add(province, cityList);
                    }
                }
            }
        }

        public IReadOnlyList<string> Provinces => provinces;

        /// <summary>
        /// 根据省份获取城市列表。
        /// </summary>
        public IReadOnlyList<string> GetCities(string province)
        {
            if (province != null && cities.TryGetValue(province.Trim(), out var result)) return result;
            return Array.Empty<string>();
        }
    }
}
