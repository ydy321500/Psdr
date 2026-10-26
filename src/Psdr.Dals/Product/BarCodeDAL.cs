using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SqlSugar;

namespace Psdr.Dals.Product
{
    /// <summary>
    /// 生成并保存与新版 PSDR 条码算法兼容的明码、暗码记录。
    /// </summary>
    public sealed class BarCodeDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存访问当前代理库的连接服务。
        /// </summary>
        public BarCodeDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 读取已生成条码的最大顺序号。
        /// </summary>
        public long GetMaxIndexNo()
        {
            using (var db = database.Open())
            {
                var latest = db.Queryable<BarCode>()
                    .OrderBy(x => x.IndexNo, OrderByType.Desc)
                    .First();
                return latest?.IndexNo ?? 0;
            }
        }

        /// <summary>
        /// 为当前代理生成连续序列条码，并在同一事务内写入条码登记表。
        /// </summary>
        public List<BarCode> Generate(string agentNo, string clearCodePrefix, int amount)
        {
            if (amount <= 0 || amount > 100000) throw new InvalidOperationException("一次生成数量必须在 1 到 100000 之间。");
            var prefix = NormalizeAgentPrefix(agentNo);
            var result = new List<BarCode>();
            using (var db = database.Open())
            {
                var transaction = db.Ado.UseTran(() =>
                {
                    var latest = db.Queryable<BarCode>()
                        .OrderBy(x => x.IndexNo, OrderByType.Desc)
                        .First();
                    var start = (latest?.IndexNo ?? 0) + 1;
                    for (var offset = 0; offset < amount; offset++)
                    {
                        var index = start + offset;
                        var rawCode = prefix + index.ToString("0000000000");
                        result.Add(new BarCode
                        {
                            IndexNo = index,
                            ClearCode = BuildClearCode(clearCodePrefix, rawCode),
                            SecretCode = "BC" + Mix(rawCode + Md5(rawCode).Substring(0, 8), 2),
                            Time = DateTime.Now,
                            State = 0
                        });
                    }
                    db.Insertable(result).ExecuteCommand();
                });
                if (!transaction.IsSuccess) throw transaction.ErrorException;
            }
            return result;
        }

        private static string NormalizeAgentPrefix(string agentNo)
        {
            var value = new string((agentNo ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            if (value.Length < 6) throw new InvalidOperationException("当前代理号无法生成六码条码前缀。");
            return value.Substring(0, 6).ToUpperInvariant();
        }

        private static string BuildClearCode(string clearCodePrefix, string rawCode)
        {
            var prefix = (clearCodePrefix ?? string.Empty).Trim().TrimEnd('@');
            return string.IsNullOrWhiteSpace(prefix) ? rawCode : prefix + "@" + rawCode;
        }

        private static string Md5(string value)
        {
            using (var md5 = MD5.Create())
            {
                return string.Concat(md5.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(x => x.ToString("x2")));
            }
        }

        private static string Mix(string input, int segmentLength)
        {
            var first = new StringBuilder();
            var second = new StringBuilder();
            while (input.Length >= segmentLength * 2)
            {
                first.Append(input.Substring(0, segmentLength));
                input = input.Substring(segmentLength);
                second.Append(input.Substring(0, segmentLength));
                input = input.Substring(segmentLength);
            }
            first.Append(input);
            return first + second.ToString();
        }
    }
}
