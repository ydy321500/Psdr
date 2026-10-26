using System;
using SqlSugar;

namespace Psdr.Models.Entities
{
    /// <summary>Agent function entitlement mirrored into the tenant database.</summary>
    [SugarTable("TenantFunction")]
    public sealed class TenantFunction
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long FunctionID { get; set; }
        public string FunctionName { get; set; }
        public DateTime StopTime { get; set; }
        public int FunctionType { get; set; }
        public int State { get; set; }
    }
}
