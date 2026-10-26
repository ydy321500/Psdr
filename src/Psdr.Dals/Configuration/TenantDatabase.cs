using SqlSugar;
using System;
using System.Data.SqlClient;

namespace Psdr.Dals.Configuration
{
    /// <summary>
    /// Opens only the configured tenant database. No central database credentials are distributed.
    /// </summary>
    public sealed class TenantDatabase
    {
        private readonly string suppliedConnectionString;
        private readonly AgentSettingsStore agentSettings;

        /// <summary>
            /// 创建使用本机已保存代理编号的租户数据库连接服务。
        /// </summary>
        public TenantDatabase() : this(new AgentSettingsStore())
        {
        }

        /// <summary>
            /// 创建使用指定代理编号存储服务的租户数据库连接服务。
        /// </summary>
        public TenantDatabase(AgentSettingsStore agentSettings)
        {
            this.agentSettings = agentSettings;
        }

        /// <summary>
        /// Allows an isolated connection to be supplied by integration tests.
        /// </summary>
        public static TenantDatabase ForConnectionString(string connectionString)
        {
            return new TenantDatabase(connectionString);
        }

        /// <summary>
            /// 创建仅供集成测试使用的固定连接字符串实例。
        /// </summary>
        private TenantDatabase(string connectionString)
        {
            suppliedConnectionString = connectionString;
        }

        /// <summary>
        /// 打开数据库连接。
        /// </summary>
        public SqlSugarClient Open()
        {
            var connectionString = suppliedConnectionString ?? BuildConnectionString();
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });
        }

        public string CurrentDatabaseName
        {
            get
            {
                return agentSettings?.Load() ?? "未设置代理号";
            }
        }

        /// <summary>
        /// 根据数据库配置生成连接字符串。
        /// </summary>
        private string BuildConnectionString()
        {
            var server = Environment.GetEnvironmentVariable("PSDR_SQL_SERVER") ?? ".";
            var agentNo = agentSettings?.Load();
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(agentNo))
            {
                throw new InvalidOperationException("未设置代理号或数据库服务器，请检查代理设置。");
            }

            var connection = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = agentNo,
                UserID = agentNo,
                Password = agentNo,
                Encrypt = true,
                TrustServerCertificate =
                    Environment.GetEnvironmentVariable("PSDR_SQL_TRUST_SERVER_CERTIFICATE") == "1" ||
                    (Environment.GetEnvironmentVariable("PSDR_SQL_TRUST_SERVER_CERTIFICATE") == null && server == "."),
                PersistSecurityInfo = false,
                ConnectTimeout = 5
            };

            return connection.ConnectionString;
        }
    }
}
