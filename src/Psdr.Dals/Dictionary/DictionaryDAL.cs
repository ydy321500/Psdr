using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Dictionary
{
        /// <summary>Maintains product and business dictionary values in the tenant database.</summary>
        public sealed class DictionaryDAL
        {
        private readonly TenantDatabase tenantDatabase;
        
        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public DictionaryDAL(TenantDatabase tenantDatabase)
        {
            this.tenantDatabase = tenantDatabase;
        }

        /// <summary>
            /// 查询指定产品下按类别、属性和内容排序的全部字典项。
        /// </summary>
        public List<DictionaryTable> GetAll(string productName)
        {
            using (var db = tenantDatabase.Open())
            {
                RequireProduct(db, productName);
                return db.Queryable<DictionaryTable>().Where(x => x.ProductName == productName)
                    .OrderBy(x => x.DicModel).OrderBy(x => x.DicName).OrderBy(x => x.DicValue)
                    .ToList();
            }
        }

        /// <summary>
        /// 查询指定商品启用的产品属性。
        /// </summary>
        public List<Psdr.Models.Entities.ProductMap> GetEnabledAttributes(string productName)
        {
            using (var db = tenantDatabase.Open())
            {
                RequireProduct(db, productName);
                return db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .Where(x => x.ProductName == productName && x.State == 1 && x.ValueName != "")
                    .OrderBy(x => x.KeyName).ToList();
            }
        }

        /// <summary>
            /// 新增或更新指定产品的业务字典项，并校验产品属性和重复内容。
        /// </summary>
        public bool Save(DictionaryTable item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            item.DicName = (item.DicName ?? string.Empty).Trim();
            item.DicValue = (item.DicValue ?? string.Empty).Trim();
            if (item.DicModel != "产品" && item.DicModel != "业务")
                throw new InvalidOperationException("请选择字典类别。");
            if (item.DicName.Length == 0 || item.DicValue.Length == 0)
                throw new InvalidOperationException("属性和内容不能为空。");
            using (var db = tenantDatabase.Open())
            {
                RequireProduct(db, item.ProductName);
                if (item.DicModel == "产品" && !db.Queryable<Psdr.Models.Entities.ProductMap>()
                    .Any(x => x.ProductName == item.ProductName && x.KeyName == item.DicName
                        && x.State == 1 && x.ValueName != ""))
                    throw new InvalidOperationException("请先启用对应的产品属性。");
                if (db.Queryable<DictionaryTable>().Any(x => x.ProductName == item.ProductName
                    && x.DicModel == item.DicModel && x.DicName == item.DicName
                    && x.DicValue == item.DicValue && x.DicID != item.DicID))
                    throw new InvalidOperationException("同一属性下已存在该字典内容。");
                if (item.DicID == 0) return db.Insertable(item).ExecuteCommand() == 1;
                if (!db.Queryable<DictionaryTable>()
                    .Any(x => x.DicID == item.DicID && x.ProductName == item.ProductName))
                    return false;
                return db.Updateable(item).ExecuteCommand() == 1;
            }
        }

        /// <summary>
            /// 删除指定产品下的一个字典项。
        /// </summary>
        public bool Delete(int id, string productName)
        {
            using (var db = tenantDatabase.Open())
            {
                RequireProduct(db, productName);
                return db.Deleteable<DictionaryTable>()
                    .Where(x => x.DicID == id && x.ProductName == productName)
                    .ExecuteCommand() == 1;
            }
        }

        /// <summary>
        /// 校验指定商品存在并返回其记录。
        /// </summary>
        private static void RequireProduct(SqlSugarClient db, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !db.Queryable<TenantFunction>()
                .Any(x => x.FunctionName == name && x.FunctionType == 0 && x.State == 1
                    && x.StopTime >= DateTime.Now))
                throw new InvalidOperationException("产品功能未启用、已过期或尚未配置。");
        }
    }
}
