namespace Psdr.Admin.Services
{
    public sealed class AdminSession
    {
        public string DisplayName { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(DisplayName);

        /// <summary>
        /// 记录管理员登录信息并更新认证状态。
        /// </summary>
        public void SignIn(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
