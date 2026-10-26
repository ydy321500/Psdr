using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("ReportStyle")]
    public class ReportStyle
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public ReportStyle()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ReportID { get; set; }

        public string ReportName { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public byte[] ReportContent { get; set; }
    }
}
