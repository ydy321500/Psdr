using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("FinanceSupply")]
    public partial class FinanceSupply
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public FinanceSupply()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long FinanceID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? SupplyID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        public decimal? Money { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? Time { get; set; }

        /// <summary>
        /// Desc:0-记账 1-销账
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? Type { get; set; }

        ///// <summary>
        ///// Desc:
        ///// Default:
        ///// Nullable:True
        ///// </summary>           
        //public string Name { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string No { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public decimal? Postage { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int State { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Remark { get; set; }

    }
}
