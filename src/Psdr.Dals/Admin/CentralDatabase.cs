using SqlSugar;
using System;
using System.Data.SqlClient;

namespace Psdr.Dals.Admin
{
    /// <summary>
    /// Connects to the central registry from the separately deployed administrator application.
    /// </summary>
    public sealed class CentralDatabase
    {
        public string Server => Environment.GetEnvironmentVariable("PSDR_ADMIN_SQL_SERVER") ?? ".";
        public string DatabaseName => Environment.GetEnvironmentVariable("PSDR_ADMIN_DATABASE") ?? "Psdr_Master";

        /// <summary>
        /// 打开数据库连接。
        /// </summary>
        public SqlConnection Open(string databaseName = null)
        {
            var connection = new SqlConnection(BuildConnectionString(databaseName ?? DatabaseName));
            connection.Open();
            return connection;
        }

        /// <summary>
        /// 创建并打开数据库访问客户端。
        /// </summary>
        public SqlSugarClient OpenClient()
        {
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = BuildConnectionString(DatabaseName),
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });
        }

        /// <summary>
        /// 确保主库具备代理建库和租户初始化所需的权限。
        /// </summary>
        public void EnsureProvisioningRights()
        {
            using (var connection = Open("master"))
            using (var command = new SqlCommand("SELECT IS_SRVROLEMEMBER(N'sysadmin')", connection))
            {
                if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                    throw new InvalidOperationException("当前 Windows 账号没有代理建库权限。请使用授权的管理账号运行。");
            }
        }

        /// <summary>
        /// 根据数据库配置生成连接字符串。
        /// </summary>
        public string BuildConnectionString(string databaseName)
        {
            var trustSetting = Environment.GetEnvironmentVariable("PSDR_ADMIN_TRUST_SERVER_CERTIFICATE");
            return new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = databaseName,
                IntegratedSecurity = true,
                Encrypt = true,
                TrustServerCertificate = trustSetting == "1" ||
                    (trustSetting == null && Server == "."),
                PersistSecurityInfo = false,
                ConnectTimeout = 5
            }.ConnectionString;
        }
    }
}
