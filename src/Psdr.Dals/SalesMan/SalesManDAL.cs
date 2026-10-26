using System.Collections.Generic;
using System;
using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;

namespace Psdr.Dals.SalesMan
{
        /// <summary>Maintains salespeople in the current tenant database.</summary>
        public sealed class SalesManDAL
        {
        private readonly TenantDatabase database;
        
        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public SalesManDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
            /// 按编号倒序查询当前代理的全部销售人员资料。
        /// </summary>
        public List<Psdr.Models.Entities.SalesMan> GetAll()
        {
            using (var db = database.Open())
                return db.Queryable<Psdr.Models.Entities.SalesMan>()
                    .OrderBy(x => x.SalesID, OrderByType.Desc).ToList();
        }

        /// <summary>
            /// 新增或更新销售人员；改名时同步更新引用该人员的客户资料。
        /// </summary>
        public bool Save(Psdr.Models.Entities.SalesMan item)
        {
            using (var db = database.Open())
            {
                if (db.Queryable<Psdr.Models.Entities.SalesMan>()
                    .Any(x => x.Name == item.Name && x.SalesID != item.SalesID)) return false;
                if (item.SalesID == 0) return db.Insertable(item).ExecuteCommand() == 1;

                var previous = db.Queryable<Psdr.Models.Entities.SalesMan>()
                    .First(x => x.SalesID == item.SalesID);
                if (previous == null) return false;

                db.Ado.BeginTran();
                try
                {
                    if (db.Updateable(item).ExecuteCommand() != 1)
                    {
                        db.Ado.RollbackTran();
                        return false;
                    }
                    if (previous.Name != item.Name)
                    {
                        db.Updateable<Psdr.Models.Entities.Shop>()
                            .SetColumns(x => new Psdr.Models.Entities.Shop { Salesman = item.Name })
                            .Where(x => x.Salesman == previous.Name).ExecuteCommand();
                    }
                    db.Ado.CommitTran();
                    return true;
                }
                catch
                {
                    db.Ado.RollbackTran();
                    throw;
                }
            }
        }

        /// <summary>
            /// 删除未被客户资料引用的销售人员。
        /// </summary>
        public bool Delete(int id)
        {
            using (var db = database.Open())
            {
                var item = db.Queryable<Psdr.Models.Entities.SalesMan>().First(x => x.SalesID == id);
                if (item == null) return false;
                if (db.Queryable<Psdr.Models.Entities.Shop>().Any(x => x.Salesman == item.Name))
                    throw new InvalidOperationException("该销售人员仍被客户使用，请先调整客户的业务员。");
                return db.Deleteable<Psdr.Models.Entities.SalesMan>()
                    .Where(x => x.SalesID == id).ExecuteCommand() == 1;
            }
        }
    }
}
