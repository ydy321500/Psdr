using Newtonsoft.Json;
using System;
using System.IO;

namespace Psdr.Desktop.Services
{
    /// <summary>
    /// Stores only the account name chosen by the user, never a password.
    /// </summary>
    public sealed class LoginPreferenceStore
    {
        private readonly string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Psdr", "login-preferences.json");

        /// <summary>
        /// 加载已保存的配置或业务数据。
        /// </summary>
        public LoginPreferences Load()
        {
            return File.Exists(path)
                ? JsonConvert.DeserializeObject<LoginPreferences>(File.ReadAllText(path)) ?? new LoginPreferences()
                : new LoginPreferences();
        }

        /// <summary>
        /// 保存当前配置或编辑后的业务数据。
        /// </summary>
        public void Save(bool rememberAccount, string userCode)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(new LoginPreferences
            {
                RememberAccount = rememberAccount,
                UserCode = rememberAccount ? userCode : null
            }));
        }
    }

    public sealed class LoginPreferences
    {
        public bool RememberAccount { get; set; }
        public string UserCode { get; set; }
    }
}
