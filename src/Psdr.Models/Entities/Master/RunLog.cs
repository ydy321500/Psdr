using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("RunLog")]
    public partial class RunLog
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public RunLog()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long LogID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string LogTitle { get; set; }
        public string LogContent { get; set; }

        /// <summary>
        /// Desc:
        /// Default:DateTime.Now
        /// Nullable:True
        /// </summary>           
        public DateTime? LogTime { get; set; }


    }
}
