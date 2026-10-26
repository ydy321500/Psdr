using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Market
{
    /// <summary>
    /// 维护市场端会员账号，供市场下单端登录与收货信息使用。
    /// </summary>
    public sealed class MarketUserDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存当前代理库连接服务。
        /// </summary>
        public MarketUserDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 查询全部市场会员账号。
        /// </summary>
        public List<M_User> GetAll()
        {
            using (var db = database.Open())
            {
                return db.Queryable<M_User>().OrderBy(x => x.UserID, OrderByType.Desc).ToList();
            }
        }

        /// <summary>
        /// 新增或修改市场会员账号，同时校验账号在代理库内唯一。
        /// </summary>
        public void Save(M_User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            user.UserCode = (user.UserCode ?? string.Empty).Trim();
            user.UserName = (user.UserName ?? string.Empty).Trim();
            if (user.UserCode.Length == 0 || user.UserName.Length == 0)
                throw new InvalidOperationException("市场账号和会员名称不能为空。");
            if (string.IsNullOrWhiteSpace(user.UserPsw))
                throw new InvalidOperationException("请输入市场账号密码。");

            using (var db = database.Open())
            {
                if (db.Queryable<M_User>().Any(x => x.UserCode == user.UserCode && x.UserID != user.UserID))
                    throw new InvalidOperationException("市场账号已存在。");
                var changed = user.UserID == 0
                    ? db.Insertable(user).ExecuteCommand()
                    : db.Updateable(user).ExecuteCommand();
                if (changed != 1) throw new InvalidOperationException("市场会员保存失败。");
            }
        }
    }
}
