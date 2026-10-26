using Psdr.Dals.Security;
using System;
using System.Data;

namespace Psdr.Dals.Admin
{
    /// <summary>
    /// Authenticates the administrator application account stored in the central database.
    /// </summary>
    public sealed class AdminAuthService
    {
        private readonly CentralDatabase database;

        /// <summary>
            /// 保存用于验证主管理员账号的主库连接服务。
        /// </summary>
        public AdminAuthService(CentralDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 验证账号和密码，返回匹配的用户信息。
        /// </summary>
        public string Authenticate(string userCode, string password)
        {
            if (string.IsNullOrWhiteSpace(userCode) || string.IsNullOrEmpty(password)) return null;
            using (var connection = database.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT DisplayName, PasswordHash FROM dbo.AdminUser
                    WHERE UserCode=@code AND IsEnabled=1";
                command.Parameters.Add("@code", SqlDbType.NVarChar, 50).Value = userCode.Trim();
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    var displayName = reader.GetString(0);
                    var hash = reader.GetString(1);
                    return PasswordHasher.Verify(password, hash) ? displayName : null;
                }
            }
        }
    }
}
