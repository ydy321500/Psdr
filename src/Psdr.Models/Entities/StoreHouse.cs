using SqlSugar;

namespace Psdr.Models.Entities
{
    /// <summary>
    /// Maps the existing tenant database StoreHouse table.
    /// </summary>
    [SugarTable("StoreHouse")]
    public sealed class StoreHouse
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int StoreHouseID { get; set; }

        public string Name { get; set; }

        public string Remark { get; set; }
    }
}
