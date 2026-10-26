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
    [SugarTable("Stock")]
    public partial class Stock
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public Stock()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long StockID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? ProductID { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string BarCode { get; set; }


        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>    
        public int? Amount { get; set; }

        [SugarColumn(IsIgnore =true)]
        public decimal? PurchasePrice { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K1 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K2 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K3 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K4 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K5 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>         
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K6 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K7 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string K8 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V1 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V2 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V3 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>          
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V4 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V5 { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>          
        /// 
        [SugarColumn(IsIgnore = true)]
        public string V6 { get; set; }

        /// <summary>
        /// 便于删除
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int StockIndex { get; set; }

    }
}
