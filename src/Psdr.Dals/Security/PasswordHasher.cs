using System;
using System.Security.Cryptography;

namespace Psdr.Dals.Security
{
    /// <summary>
    /// Hashes business passwords with a per-user salt.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100000;
        private const int SaltLength = 16;
        private const int HashLength = 32;

        /// <summary>
        /// 使用安全哈希算法生成密码摘要。
        /// </summary>
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentException("密码不能为空。", nameof(password));
            var salt = new byte[SaltLength];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return "PBKDF2-SHA256$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" +
                    Convert.ToBase64String(derive.GetBytes(HashLength));
            }
        }

        /// <summary>
        /// 校验密码与已保存的密码摘要是否匹配。
        /// </summary>
        public static bool Verify(string password, string encoded)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encoded)) return false;
            var parts = encoded.Split('$');
            if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256" ||
                !int.TryParse(parts[1], out var iterations) || iterations < 100000 || iterations > 1000000)
                return false;
            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                if (salt.Length != SaltLength || expected.Length != HashLength) return false;
                using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    var actual = derive.GetBytes(expected.Length);
                    var difference = 0;
                    for (var i = 0; i < expected.Length; i++) difference |= actual[i] ^ expected[i];
                    return difference == 0;
                }
            }
            catch (FormatException) { return false; }
        }
    }
}
