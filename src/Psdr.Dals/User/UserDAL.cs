using Psdr.Dals.Configuration;
using Psdr.Dals.Security;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using SqlSugar;

namespace Psdr.Dals.User
{
    /// <summary>
    /// Authenticates business users inside the selected tenant database.
    /// </summary>
    public sealed class UserDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
            /// 保存用于访问当前代理库的数据库连接服务。
        /// </summary>
        public UserDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 验证账号和密码，返回匹配的用户信息。
        /// </summary>
        public Models.Entities.User Authenticate(string userCode, string password)
        {
            if (string.IsNullOrWhiteSpace(userCode) || string.IsNullOrEmpty(password)) return null;
            using (var client = database.Open())
            {
                var code = userCode.Trim();
                var user = client.Queryable<Models.Entities.User>().First(x => x.UserCode == code);
                if (user == null || user.State != 1 || !PasswordHasher.Verify(password, user.UserPassword)) return null;
                user.UserPassword = null;
                return user;
            }
        }

        /// <summary>
            /// 查询当前代理的全部业务用户，且不返回密码散列。
        /// </summary>
        public List<Models.Entities.User> GetAll()
        {
            using (var client = database.Open())
                return client.Queryable<Models.Entities.User>()
                    .Select(x => new Models.Entities.User
                    {
                        UserID = x.UserID, UserCode = x.UserCode, UserName = x.UserName,
                        Role = x.Role, State = x.State, Online = x.Online
                    })
                    .OrderBy(x => x.UserID, OrderByType.Desc).ToList();
        }

        /// <summary>
            /// 新增业务用户并初始化密码，或更新既有用户的资料、权限和状态。
        /// </summary>
        public bool Save(Models.Entities.User item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var code = (item.UserCode ?? string.Empty).Trim();
            if (code.Length == 0 || string.Equals(code, "sys", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("账号不能为空，也不能使用主管理员保留账号 sys。");

            using (var client = database.Open())
            {
                if (client.Queryable<Models.Entities.User>()
                    .Any(x => x.UserCode == code && x.UserID != item.UserID)) return false;

                if (item.UserID == 0)
                {
                    item.UserCode = code;
                    item.UserPassword = PasswordHasher.Hash(DefaultUserCredentials.Password);
                    return client.Insertable(item).ExecuteCommand() == 1;
                }

                var existing = client.Queryable<Models.Entities.User>().First(x => x.UserID == item.UserID);
                if (existing == null) return false;
                if (string.Equals(existing.UserCode, "admin", StringComparison.OrdinalIgnoreCase) &&
                    (item.State != 1 || !string.Equals(code, "admin", StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("代理管理员 admin 不能停用或更改账号。");

                return client.Updateable<Models.Entities.User>()
                    .SetColumns(x => new Models.Entities.User
                    {
                        UserCode = code, UserName = item.UserName, Role = item.Role, State = item.State
                    })
                    .Where(x => x.UserID == item.UserID).ExecuteCommand() == 1;
            }
        }

        /// <summary>
            /// 删除指定业务用户，内置 admin 用户不可删除。
        /// </summary>
        public bool Delete(int id)
        {
            using (var client = database.Open())
            {
                var existing = client.Queryable<Models.Entities.User>().First(x => x.UserID == id);
                if (existing == null) return false;
                if (string.Equals(existing.UserCode, "admin", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("代理管理员 admin 不能删除。");
                return client.Deleteable<Models.Entities.User>()
                    .Where(x => x.UserID == id).ExecuteCommand() == 1;
            }
        }
    }
}
