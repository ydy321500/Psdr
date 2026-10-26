using Psdr.Dals.Configuration;
using Psdr.Models.Entities;
using SqlSugar;
using System;
using System.Collections.Generic;

namespace Psdr.Dals.Report
{
    /// <summary>
    /// 维护存放在代理库中的 FastReport 报表模板内容。
    /// </summary>
    public sealed class ReportStyleDAL
    {
        private readonly TenantDatabase database;

        /// <summary>
        /// 保存当前代理库连接服务。
        /// </summary>
        public ReportStyleDAL(TenantDatabase database)
        {
            this.database = database;
        }

        /// <summary>
        /// 读取全部模板元数据与二进制内容。
        /// </summary>
        public List<ReportStyle> GetAll()
        {
            using (var db = database.Open())
            {
                return db.Queryable<ReportStyle>().OrderBy(x => x.ReportID, OrderByType.Desc).ToList();
            }
        }

        /// <summary>
        /// 新增或更新一个报表模板，并按名称防止重复。
        /// </summary>
        public void Save(ReportStyle style)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            style.ReportName = (style.ReportName ?? string.Empty).Trim();
            if (style.ReportName.Length == 0) throw new InvalidOperationException("报表模板名称不能为空。");
            if (style.ReportContent == null || style.ReportContent.Length == 0)
                throw new InvalidOperationException("请选择报表模板文件。");
            using (var db = database.Open())
            {
                if (db.Queryable<ReportStyle>().Any(x => x.ReportName == style.ReportName && x.ReportID != style.ReportID))
                    throw new InvalidOperationException("已存在同名报表模板。");
                var changed = style.ReportID == 0
                    ? db.Insertable(style).ExecuteCommand()
                    : db.Updateable(style).ExecuteCommand();
                if (changed != 1) throw new InvalidOperationException("报表模板保存失败。");
            }
        }

        /// <summary>
        /// 删除指定报表模板。
        /// </summary>
        public void Delete(int reportId)
        {
            using (var db = database.Open())
            {
                if (db.Deleteable<ReportStyle>().Where(x => x.ReportID == reportId).ExecuteCommand() != 1)
                    throw new InvalidOperationException("报表模板不存在。");
            }
        }
    }
}
