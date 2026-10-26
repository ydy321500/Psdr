using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Psdr.Dals.ProductMap
{
        /// <summary>Reads product entitlements and edits their tenant-local attribute mappings.</summary>
        public sealed class ProductMapDAL
        {
        private readonly TenantDatabase tenantDatabase;
        
        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public ProductMapDAL(TenantDatabase tenantDatabase)
        {
            this.tenantDatabase = tenantDatabase;
        }

        /// <summary>
        /// 查询当前启用的商品列表。
        /// </summary>
        public List<TenantFunction> GetActiveProducts()
        {
            using (var db = tenantDatabase.Open())
            {
                if (Convert.ToInt32(db.Ado.GetScalar(
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.TenantFunction', N'U') IS NULL THEN 0 ELSE 1 END")) == 0)
                    throw new InvalidOperationException(
                        "当前代理库缺少产品功能表，请检查代理建库脚本是否已完整执行。");
                return db.Queryable<TenantFunction>()
                    .Where(x => x.FunctionType == 0 && x.State == 1 && x.StopTime >= DateTime.Now)
                    .OrderBy(x => x.FunctionName)
                    .ToList();
            }
        }

        /// <summary>
        /// 查询产品属性映射数据。
        /// </summary>
        public List<Psdr.Models.Entities.ProductMap> GetMaps(string productName)
        {
            using (var db = tenantDatabase.Open())
            {
                RequireActiveProduct(db, productName);
                return db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .Where(x => x.ProductName == productName)
                    .ToList()
                    .OrderBy(x => x.KeyName != null && x.KeyName.StartsWith("K") ? 0 : 1)
                    .ThenBy(x => int.TryParse((x.KeyName ?? string.Empty).Length > 1
                        ? x.KeyName.Substring(1) : string.Empty, out var number)
                        ? number : int.MaxValue)
                    .ToList();
            }
        }

        /// <summary>
            /// 更新指定产品属性的名称、必填规则和启用状态。
        /// </summary>
        public bool Save(int mapId, string productName, string valueName, string fieldType, bool enabled)
        {
            valueName = (valueName ?? string.Empty).Trim();
            if (valueName.Length > 100) throw new InvalidOperationException("属性名称不能超过 100 个字符。");
            if (enabled && valueName.Length == 0) throw new InvalidOperationException("启用的属性必须填写名称。");
            if (fieldType != "必填" && fieldType != "选填")
                throw new InvalidOperationException("属性类型必须是必填或选填。");

            using (var db = tenantDatabase.Open())
            {
                RequireActiveProduct(db, productName);
                var item = db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .First(x => x.MapID == mapId && x.ProductName == productName);
                if (item == null) return false;
                if (valueName.Length > 0 && db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .Any(x => x.ProductName == productName && x.ValueName == valueName && x.MapID != mapId))
                    throw new InvalidOperationException("该产品已有同名属性。");

                item.ValueName = valueName;
                item.FieldType = fieldType;
                item.State = enabled ? 1 : 0;
                return db.Updateable(item).UpdateColumns(x => new { x.ValueName, x.FieldType, x.State })
                    .ExecuteCommand() == 1;
            }
        }

        /// <summary>
        /// 校验指定商品存在且处于启用状态。
        /// </summary>
        private static void RequireActiveProduct(SqlSugarClient db, string productName)
        {
            if (string.IsNullOrWhiteSpace(productName) || !db.Queryable<TenantFunction>()
                .Any(x => x.FunctionName == productName && x.FunctionType == 0 && x.State == 1
                    && x.StopTime >= DateTime.Now))
                throw new InvalidOperationException("产品功能未启用、已过期或尚未配置，请联系主管理员。");
        }
    }
}
