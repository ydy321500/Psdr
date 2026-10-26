using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Psdr.Dals.Admin
{
    /// <summary>
    /// Keeps the master entitlement, tenant entitlement and product field definitions consistent.
    /// Only the administrator connection performs cross-database writes.
    /// </summary>
    public sealed class AgentFunctionService
    {
        private readonly CentralDatabase database;
        private readonly AgentDAL agents;

        /// <summary>
            /// 保存主库连接和代理登记服务，以维护功能授权。
        /// </summary>
        public AgentFunctionService(CentralDatabase database, AgentDAL agents)
        {
            this.database = database;
            this.agents = agents;
        }

        /// <summary>
            /// 查询指定代理已登记的全部功能授权。
        /// </summary>
        public List<AgentFunction> GetAll(Guid agentId)
        {
            using (var client = database.OpenClient())
                return client.Queryable<AgentFunction>().Where(x => x.AgentID == agentId)
                    .OrderBy(x => x.FunctionID).ToList();
        }

        /// <summary>
            /// 保存代理功能授权，并在同一事务内写入代理库及初始化产品属性映射。
        /// </summary>
        public void Save(AgentFunction item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            EnsureReady(item.AgentID);
            var name = (item.FunctionName ?? string.Empty).Trim();
            if (name.Length == 0 || name.Length > 100)
                throw new InvalidOperationException("功能名称长度应为 1 到 100 个字符。");
            if (item.FunctionType != 0 && item.FunctionType != 1)
                throw new InvalidOperationException("功能类型只能是产品或模块。");
            var tenant = TenantName(item.AgentID);

            using (var connection = database.Open())
            using (var transaction = connection.BeginTransaction())
            {
                if (Scalar<int>(connection, transaction,
                    "SELECT COUNT(*) FROM dbo.AgentFunction WHERE AgentID=@agent AND FunctionName=@name AND FunctionID<>@id",
                    Param("@agent", SqlDbType.UniqueIdentifier, item.AgentID),
                    Param("@name", SqlDbType.NVarChar, name, 100),
                    Param("@id", SqlDbType.BigInt, item.FunctionID)) > 0)
                    throw new InvalidOperationException("该代理已有同名功能。");

                if (item.FunctionID == 0)
                {
                    item.FunctionID = Scalar<long>(connection, transaction,
                        "INSERT dbo.AgentFunction (AgentID,FunctionName,StopTime,FunctionType,State) " +
                        "OUTPUT INSERTED.FunctionID VALUES (@agent,@name,@stop,@type,@state)",
                        Param("@agent", SqlDbType.UniqueIdentifier, item.AgentID),
                        Param("@name", SqlDbType.NVarChar, name, 100),
                        Param("@stop", SqlDbType.DateTime2, item.StopTime),
                        Param("@type", SqlDbType.Int, item.FunctionType),
                        Param("@state", SqlDbType.Int, item.State));
                }
                else
                {
                    var previous = GetExisting(connection, transaction, item.FunctionID, item.AgentID);
                    if (previous == null) throw new InvalidOperationException("代理功能不存在。");
                    if (previous.FunctionType != item.FunctionType)
                        throw new InvalidOperationException("已创建的功能不能切换产品或模块类型。");
                    if (previous.FunctionName != name && item.FunctionType == 0)
                    {
                        if (Scalar<int>(connection, transaction,
                            "SELECT COUNT(*) FROM " + tenant + ".dbo.Product WHERE ProductName=@old",
                            Param("@old", SqlDbType.NVarChar, previous.FunctionName, 100)) > 0)
                            throw new InvalidOperationException("该产品类型已有产品资料，不能改名。");
                        Execute(connection, transaction,
                            "UPDATE " + tenant + ".dbo.ProductMap SET ProductName=@name WHERE ProductName=@old; " +
                            "UPDATE " + tenant + ".dbo.[Dictionary] SET ProductName=@name WHERE ProductName=@old",
                            Param("@name", SqlDbType.NVarChar, name, 100),
                            Param("@old", SqlDbType.NVarChar, previous.FunctionName, 100));
                    }
                    Execute(connection, transaction,
                        "UPDATE dbo.AgentFunction SET FunctionName=@name, StopTime=@stop, State=@state " +
                        "WHERE FunctionID=@id AND AgentID=@agent",
                        Param("@name", SqlDbType.NVarChar, name, 100),
                        Param("@stop", SqlDbType.DateTime2, item.StopTime),
                        Param("@state", SqlDbType.Int, item.State),
                        Param("@id", SqlDbType.BigInt, item.FunctionID),
                        Param("@agent", SqlDbType.UniqueIdentifier, item.AgentID));
                }

                item.FunctionName = name;
                UpsertTenant(connection, transaction, tenant, item);
                if (item.FunctionType == 0) SeedProductMap(connection, transaction, tenant, name);
                transaction.Commit();
            }
        }

        /// <summary>
        /// 校验代理及租户数据库已准备就绪。
        /// </summary>
        private void EnsureReady(Guid agentId)
        {
            var agent = agents.Get(agentId);
            if (agent == null || agent.ProvisioningStatus != "Ready")
                throw new InvalidOperationException("只有已完成建库的代理可以设置功能。");
        }

        /// <summary>
        /// 查询租户中已存在的功能权限记录。
        /// </summary>
        private static AgentFunction GetExisting(SqlConnection connection, SqlTransaction transaction,
            long functionId, Guid agentId)
        {
            using (var command = new SqlCommand(
                "SELECT FunctionName,FunctionType FROM dbo.AgentFunction WHERE FunctionID=@id AND AgentID=@agent",
                connection, transaction))
            {
                command.Parameters.Add(Param("@id", SqlDbType.BigInt, functionId));
                command.Parameters.Add(Param("@agent", SqlDbType.UniqueIdentifier, agentId));
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new AgentFunction { FunctionName = reader.GetString(0), FunctionType = reader.GetInt32(1) };
                }
            }
        }

        /// <summary>
        /// 新增或更新租户数据库中的功能权限记录。
        /// </summary>
        private static void UpsertTenant(SqlConnection connection, SqlTransaction transaction, string tenant,
            AgentFunction item)
        {
            Execute(connection, transaction,
                "IF EXISTS (SELECT 1 FROM " + tenant + ".dbo.TenantFunction WHERE FunctionID=@id) " +
                "UPDATE " + tenant + ".dbo.TenantFunction SET FunctionName=@name, StopTime=@stop, " +
                "FunctionType=@type, State=@state WHERE FunctionID=@id " +
                "ELSE INSERT " + tenant + ".dbo.TenantFunction " +
                "(FunctionID,FunctionName,StopTime,FunctionType,State) VALUES (@id,@name,@stop,@type,@state)",
                Param("@id", SqlDbType.BigInt, item.FunctionID),
                Param("@name", SqlDbType.NVarChar, item.FunctionName, 100),
                Param("@stop", SqlDbType.DateTime2, item.StopTime),
                Param("@type", SqlDbType.Int, item.FunctionType),
                Param("@state", SqlDbType.Int, item.State));
        }

        /// <summary>
        /// 向租户数据库初始化默认产品属性映射。
        /// </summary>
        private static void SeedProductMap(SqlConnection connection, SqlTransaction transaction,
            string tenant, string productName)
        {
            for (var index = 1; index <= 14; index++)
            {
                var key = index <= 8 ? "K" + index : "V" + (index - 8);
                Execute(connection, transaction,
                    "IF NOT EXISTS (SELECT 1 FROM " + tenant + ".dbo.ProductMap " +
                    "WHERE ProductName=@name AND KeyName=@key) " +
                    "INSERT " + tenant + ".dbo.ProductMap " +
                    "(ProductName,KeyName,ValueName,FieldType,State) VALUES (@name,@key,N'',@fieldType,0)",
                    Param("@name", SqlDbType.NVarChar, productName, 100),
                    Param("@key", SqlDbType.NVarChar, key, 3),
                    Param("@fieldType", SqlDbType.NVarChar, index <= 8 ? "必填" : "选填", 10));
            }
        }

        /// <summary>
        /// 根据代理编号生成租户数据库名称。
        /// </summary>
        private static string TenantName(Guid agentId)
        {
            if (agentId == Guid.Empty) throw new ArgumentException("代理号无效。", nameof(agentId));
            return "[" + agentId.ToString("D") + "]";
        }

        /// <summary>
        /// 创建带类型和值的 SQL 参数。
        /// </summary>
        private static SqlParameter Param(string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = new SqlParameter(name, type) { Value = value };
            if (size > 0) parameter.Size = size;
            return parameter;
        }

        /// <summary>
        /// 在指定事务中执行查询并转换单个标量结果。
        /// </summary>
        private static T Scalar<T>(SqlConnection connection, SqlTransaction transaction,
            string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                return (T)Convert.ChangeType(command.ExecuteScalar(), typeof(T));
            }
        }

        /// <summary>
        /// 在指定事务中执行 SQL 命令。
        /// </summary>
        private static void Execute(SqlConnection connection, SqlTransaction transaction,
            string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                command.ExecuteNonQuery();
            }
        }
    }
}
