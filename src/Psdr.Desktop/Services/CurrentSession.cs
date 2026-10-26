using Psdr.Models.Entities;
using System;
using System.Linq;

namespace Psdr.Desktop.Services
{
    /// <summary>
    /// Holds only the signed-in business identity for the current desktop session.
    /// </summary>
    public sealed class CurrentSession
    {
        public string UserCode { get; private set; }
        public string DisplayName { get; private set; }
        public string Role { get; private set; }
        public bool IsBusinessAdministrator => string.Equals(UserCode, "admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 判断当前会话用户是否拥有指定业务权限。
        /// </summary>
        public bool CanAccess(string permission)
        {
            return IsBusinessAdministrator || (Role ?? string.Empty).Split('|')
                .Any(value => string.Equals(value, permission, StringComparison.Ordinal));
        }

        /// <summary>
        /// 记录管理员登录信息并更新认证状态。
        /// </summary>
        public void SignIn(User user)
        {
            UserCode = user.UserCode;
            DisplayName = string.IsNullOrWhiteSpace(user.UserName) ? user.UserCode : user.UserName;
            Role = user.Role;
        }
    }
}
