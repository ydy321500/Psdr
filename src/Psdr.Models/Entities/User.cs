using SqlSugar;

namespace Psdr.Models.Entities
{
    /// <summary>
    /// Business user in the current tenant database.
    /// </summary>
    [SugarTable("User")]
    public sealed class User
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int UserID { get; set; }

        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public string Role { get; set; }
        public int? State { get; set; }
        public int? Online { get; set; }
    }
}
