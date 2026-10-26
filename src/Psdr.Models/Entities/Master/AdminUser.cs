using SqlSugar;

namespace Psdr.Models.Entities
{
    /// <summary>
    /// Administrator login stored in the master database.
    /// </summary>
    [SugarTable("AdminUser")]
    public sealed class AdminUser
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int AdminUserID { get; set; }

        public string UserCode { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHash { get; set; }
        public bool IsEnabled { get; set; }
    }
}
