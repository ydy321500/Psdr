using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("StorehouseRecord")]
    public class StorehouseRecord
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public StorehouseRecord()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsIdentity = true)]
        public long StorehouseRecordID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true)]
        public string No { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? SupplyID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? Time { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string ProductName { get; set; }
        public int? Amount { get; set; }
        public decimal? TotalMoney { get; set; }
        public decimal? OtherMoney { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Remark { get; set; }

        /// <summary>
        /// Desc:默认 = 0,已提交 = 1,已审核 = 2,已作废 = 3,已兑换=4,已退货=5
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? State { get; set; }
    }
}
