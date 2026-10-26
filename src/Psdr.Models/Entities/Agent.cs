using SqlSugar;
using System;

namespace Psdr.Models.Entities
{
    /// <summary>
    /// Agent registered in the central database.
    /// </summary>
    [SugarTable("Agent")]
    public sealed class Agent
    {
        [SugarColumn(IsPrimaryKey = true)]
        public Guid AgentID { get; set; }
        public string AgentName { get; set; }
        public string Contact { get; set; }
        public string Tel { get; set; }
        public string AgentAddress { get; set; }
        public string AgentProvince { get; set; }
        public string AgentCity { get; set; }
        public string AgentArea { get; set; }
        public string BankNo { get; set; }
        public string BankName { get; set; }
        public int PayType { get; set; }
        public int State { get; set; }
        public DateTime CreatTime { get; set; }
        public string WeiXin { get; set; }
        public string Mchid { get; set; }
        public string Appid { get; set; }
        public string Appsecret { get; set; }
        public string FyPsdrLeft { get; set; }
        public int NeedCheck { get; set; }
        public int MaxScanCount { get; set; }
        public string ProvisioningStatus { get; set; }
        public string ProvisioningError { get; set; }
    }
}
