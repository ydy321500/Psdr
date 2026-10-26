using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Psdr.Dals.Configuration
{
    /// <summary>
    /// Persists the agent number selected on this workstation.
    /// </summary>
    public sealed class AgentSettingsStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Psdr.AgentNo.v1");
        private readonly string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Psdr", "agent-number.dat");

        /// <summary>
        /// 加载已保存的配置或业务数据。
        /// </summary>
        public string Load()
        {
            if (!File.Exists(path)) return null;
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(plain).Trim(); }
            finally { Array.Clear(plain, 0, plain.Length); }
        }

        /// <summary>
            /// 使用当前 Windows 用户的 DPAPI 密钥加密保存代理编号。
        /// </summary>
        public void Save(string agentNo)
        {
            if (!Guid.TryParse(agentNo, out _))
                throw new ArgumentException("请输入有效的代理号。", nameof(agentNo));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var plain = Encoding.UTF8.GetBytes(agentNo.Trim());
            try
            {
                File.WriteAllBytes(path, ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser));
            }
            finally { Array.Clear(plain, 0, plain.Length); }
        }
    }
}
