using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Psdr.Dals.Admin
{
    /// <summary>
    /// Reads and updates the central agent registry.
    /// </summary>
    public sealed class AgentDAL
    {
        private readonly CentralDatabase database;

        /// <summary>
            /// 保存用于读写主库代理登记信息的数据库连接服务。
        /// </summary>
        public AgentDAL(CentralDatabase database)
        {
            this.database = database;
        }

        /// <summary>
            /// 按创建时间倒序查询主管理端登记的全部代理。
        /// </summary>
        public List<Agent> GetAll()
        {
            using (var client = database.OpenClient())
                return client.Queryable<Agent>().OrderBy(x => x.CreatTime, SqlSugar.OrderByType.Desc).ToList();
        }

        /// <summary>
        /// 按标识查询单条数据。
        /// </summary>
        public Agent Get(Guid agentId)
        {
            using (var client = database.OpenClient())
                return client.Queryable<Agent>().First(x => x.AgentID == agentId);
        }

        /// <summary>
        /// 检查指定名称是否已被其他记录使用。
        /// </summary>
        public bool NameExists(string name, Guid? excludingId = null)
        {
            using (var connection = database.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM dbo.Agent WHERE AgentName=@name" +
                    (excludingId.HasValue ? " AND AgentID<>@id" : "");
                command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = name;
                if (excludingId.HasValue)
                    command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = excludingId.Value;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        /// <summary>
        /// 新增数据记录。
        /// </summary>
        public void Insert(Agent agent)
        {
            using (var client = database.OpenClient())
            {
                if (client.Insertable(agent).ExecuteCommand() != 1)
                    throw new InvalidOperationException("代理登记失败。");
            }
        }

        /// <summary>
        /// 更新代理的详细资料。
        /// </summary>
        public void UpdateDetails(Agent agent)
        {
            using (var connection = database.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE dbo.Agent SET AgentName=@name, Contact=@contact, Tel=@tel,
                    AgentAddress=@address, AgentProvince=@province, AgentCity=@city, PayType=@payType
                    WHERE AgentID=@id";
                AddDetails(command, agent);
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("代理资料不存在。");
            }
        }

        /// <summary>
        /// 更新代理建库流程状态及错误信息。
        /// </summary>
        public void SetProvisioningStatus(Guid agentId, string status, string error)
        {
            using (var connection = database.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE dbo.Agent SET ProvisioningStatus=@status,
                    ProvisioningError=@error WHERE AgentID=@id";
                command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = agentId;
                command.Parameters.Add("@status", SqlDbType.NVarChar, 20).Value = status;
                command.Parameters.Add("@error", SqlDbType.NVarChar, 300).Value =
                    (object)error ?? DBNull.Value;
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("代理登记状态更新失败。");
            }
        }

        /// <summary>
        /// 更新代理的启用状态。
        /// </summary>
        public void SetState(Guid agentId, int state)
        {
            using (var connection = database.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE dbo.Agent SET State=@state WHERE AgentID=@id";
                command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = agentId;
                command.Parameters.Add("@state", SqlDbType.Int).Value = state;
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("代理资料不存在。");
            }
        }

        /// <summary>
        /// 向数据库命令添加代理详细资料参数。
        /// </summary>
        private static void AddDetails(SqlCommand command, Agent agent)
        {
            command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = agent.AgentID;
            command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = agent.AgentName;
            command.Parameters.Add("@contact", SqlDbType.NVarChar, 100).Value = (object)agent.Contact ?? DBNull.Value;
            command.Parameters.Add("@tel", SqlDbType.NVarChar, 50).Value = (object)agent.Tel ?? DBNull.Value;
            command.Parameters.Add("@address", SqlDbType.NVarChar, 300).Value = (object)agent.AgentAddress ?? DBNull.Value;
            command.Parameters.Add("@province", SqlDbType.NVarChar, 100).Value = (object)agent.AgentProvince ?? DBNull.Value;
            command.Parameters.Add("@city", SqlDbType.NVarChar, 100).Value = (object)agent.AgentCity ?? DBNull.Value;
            command.Parameters.Add("@payType", SqlDbType.Int).Value = agent.PayType;
        }
    }
}
