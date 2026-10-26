using Psdr.Dals.Security;
using Psdr.Models.Entities;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

namespace Psdr.Dals.Admin
{
    public sealed class AgentCreationResult
    {
        public Guid AgentID { get; set; }
        public string InitialUserCode { get; set; }
        public string InitialPassword { get; set; }
    }

    /// <summary>
    /// Registers an agent, provisions its isolated database and SQL principal, and seeds its business login.
    /// Runs only in the administrator process with Windows SQL administrative rights.
    /// </summary>
    public sealed class TenantProvisioningService
    {
        private readonly CentralDatabase database;
        private readonly AgentDAL agents;

        /// <summary>
            /// 保存主库连接和代理登记服务，以执行分库初始化。
        /// </summary>
        public TenantProvisioningService(CentralDatabase database, AgentDAL agents)
        {
            this.database = database;
            this.agents = agents;
        }

        /// <summary>
            /// 登记新代理，并创建分库、SQL 登录账号、权限和初始业务管理员。
        /// </summary>
        public AgentCreationResult Create(Agent agent)
        {
            if (agent == null) throw new ArgumentNullException(nameof(agent));
            ValidateName(agent.AgentName);
            database.EnsureProvisioningRights();
            if (agents.NameExists(agent.AgentName.Trim()))
                throw new InvalidOperationException("已存在同名代理。");

            agent.AgentID = Guid.NewGuid();
            agent.AgentName = agent.AgentName.Trim();
            agent.CreatTime = DateTime.Now;
            agent.State = 1;
            agent.ProvisioningStatus = "Pending";
            agent.ProvisioningError = null;
            agents.Insert(agent);
            return Provision(agent.AgentID);
        }

        /// <summary>
        /// 重新执行失败的代理租户初始化流程。
        /// </summary>
        public AgentCreationResult Retry(Guid agentId)
        {
            database.EnsureProvisioningRights();
            var agent = agents.Get(agentId);
            if (agent == null) throw new InvalidOperationException("代理资料不存在。");
            if (agent.ProvisioningStatus == "Ready")
                throw new InvalidOperationException("该代理已完成建库，无需重试。");
            if (agent.ProvisioningStatus == "Unverified")
                throw new InvalidOperationException("旧代理需要先核对分库和账号，不能直接重试建库。");
            agents.SetProvisioningStatus(agentId, "Pending", null);
            return Provision(agentId);
        }

        /// <summary>
        /// 更新代理的启用状态。
        /// </summary>
        public void SetActive(Guid agentId, bool active)
        {
            database.EnsureProvisioningRights();
            var agent = agents.Get(agentId);
            if (agent == null || agent.ProvisioningStatus != "Ready")
                throw new InvalidOperationException("只有已完成建库的代理可以更改状态。");

            var identifier = QuoteIdentifier(agentId.ToString("D"));
            using (var connection = database.Open("master"))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "ALTER LOGIN " + identifier + (active ? " ENABLE" : " DISABLE");
                command.ExecuteNonQuery();
            }
            agents.SetState(agentId, active ? 1 : 0);
        }

        /// <summary>
        /// 执行代理租户数据库的完整初始化流程。
        /// </summary>
        private AgentCreationResult Provision(Guid agentId)
        {
            var name = agentId.ToString("D");
            var stage = "创建代理数据库";
            try
            {
                EnsureDatabase(name);
                stage = "创建代理 SQL 账号";
                EnsureLogin(name);
                stage = "授予代理库权限";
                EnsureDatabaseUser(name);
                stage = "初始化代理库表结构";
                ApplyTenantSchema(name);
                stage = "初始化业务管理员";
                var initialPassword = DefaultUserCredentials.Password;
                SeedBusinessAdmin(name, initialPassword);
                stage = "验证代理连接";
                VerifyAgentConnection(name);
                stage = "登记完成状态";
                agents.SetProvisioningStatus(agentId, "Ready", null);
                return new AgentCreationResult
                {
                    AgentID = agentId,
                    InitialUserCode = "admin",
                    InitialPassword = initialPassword
                };
            }
            catch (Exception ex)
            {
                var error = stage + "失败" + (ex is SqlException sql ? "（SQL " + sql.Number + "）" : "");
                try { agents.SetProvisioningStatus(agentId, "Failed", error); }
                catch { /* Preserve the original provisioning error. */ }
                throw new InvalidOperationException(error + "。可在管理端重试。", ex);
            }
        }

        /// <summary>
        /// 确保代理对应的租户数据库已创建。
        /// </summary>
        private void EnsureDatabase(string name)
        {
            using (var connection = database.Open("master"))
            {
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = "SELECT DB_ID(@name)";
                    check.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = name;
                    if (check.ExecuteScalar() != DBNull.Value) return;
                }
                using (var create = connection.CreateCommand())
                {
                    create.CommandText = "CREATE DATABASE " + QuoteIdentifier(name);
                    create.CommandTimeout = 120;
                    create.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 确保 SQL 登录名已创建并配置密码。
        /// </summary>
        private void EnsureLogin(string name)
        {
            using (var connection = database.Open("master"))
            {
                bool exists;
                using (var check = connection.CreateCommand())
                {
                    check.CommandText = "SELECT COUNT(*) FROM sys.server_principals WHERE name=@name";
                    check.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = name;
                    exists = Convert.ToInt32(check.ExecuteScalar()) != 0;
                }
                using (var command = connection.CreateCommand())
                {
                    var identifier = QuoteIdentifier(name);
                    var password = "N'" + name + "'";
                    command.CommandText = exists
                        ? "ALTER LOGIN " + identifier + " WITH PASSWORD=" + password + ", CHECK_POLICY=OFF; " +
                          "ALTER LOGIN " + identifier + " ENABLE"
                        : "CREATE LOGIN " + identifier + " WITH PASSWORD=" + password +
                          ", CHECK_POLICY=OFF, DEFAULT_DATABASE=" + identifier;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 确保租户数据库中存在对应数据库用户。
        /// </summary>
        private void EnsureDatabaseUser(string name)
        {
            using (var connection = database.Open(name))
            {
                var identifier = QuoteIdentifier(name);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "IF USER_ID(@name) IS NULL CREATE USER " + identifier +
                        " FOR LOGIN " + identifier;
                    command.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = name;
                    command.ExecuteNonQuery();
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "IF IS_ROLEMEMBER(N'db_datareader', @name) <> 1 " +
                        "ALTER ROLE db_datareader ADD MEMBER " + identifier + "; " +
                        "IF IS_ROLEMEMBER(N'db_datawriter', @name) <> 1 " +
                        "ALTER ROLE db_datawriter ADD MEMBER " + identifier;
                    command.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = name;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 在租户数据库中应用业务表结构。
        /// </summary>
        private void ApplyTenantSchema(string name)
        {
            using (var connection = database.Open(name))
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var script in new[]
                {
                    "001_StoreHouse.sql", "002_User.sql", "003_LegacyTables.sql", "004_LegacyViews.sql",
                    "005_TenantFunction.sql"
                })
                {
                    var resourceName = "Psdr.Dals.Schema.Tenant." + script;
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        if (stream == null) throw new InvalidOperationException("缺少代理库建表脚本：" + script);
                        using (var reader = new StreamReader(stream))
                            Execute(connection, transaction, reader.ReadToEnd());
                    }
                }
                transaction.Commit();
            }
        }

        /// <summary>
        /// 为租户数据库初始化业务管理员账号。
        /// </summary>
        private void SeedBusinessAdmin(string name, string password)
        {
            using (var connection = database.Open(name))
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"IF EXISTS (SELECT 1 FROM dbo.[User] WHERE UserCode=N'admin')
                    UPDATE dbo.[User] SET UserPassword=@hash, State=1 WHERE UserCode=N'admin'
                    ELSE INSERT dbo.[User] (UserCode,UserName,UserPassword,Role,State,Online)
                    VALUES (N'admin',N'代理管理员',@hash,N'代理管理',1,0)";
                command.Parameters.Add("@hash", SqlDbType.NVarChar, 300).Value = PasswordHasher.Hash(password);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 验证代理租户数据库连接是否可用。
        /// </summary>
        private void VerifyAgentConnection(string name)
        {
            var builder = new SqlConnectionStringBuilder(database.BuildConnectionString(name))
            {
                IntegratedSecurity = false,
                UserID = name,
                Password = name
            };
            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                if (!string.Equals(connection.Database, name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("代理账号连接到了错误的数据库。");
            }
        }

        /// <summary>
        /// 在指定事务中执行 SQL 命令。
        /// </summary>
        private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = 120;
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 安全转义并拼接数据库标识符。
        /// </summary>
        private static string QuoteIdentifier(string name)
        {
            if (!Guid.TryParseExact(name, "D", out _))
                throw new ArgumentException("代理标识格式无效。", nameof(name));
            return "[" + name + "]";
        }

        /// <summary>
        /// 校验数据库对象名称是否合法。
        /// </summary>
        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
                throw new ArgumentException("代理名称长度应为 1 到 100 个字符。", nameof(name));
        }
    }
}
