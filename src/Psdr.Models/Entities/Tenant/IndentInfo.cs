using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("IndentInfo")]
    public partial class IndentInfo
    {
           /// <summary>
           /// 初始化。
           /// </summary>
           public IndentInfo(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true)]
           public long OrderInfoID {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string No {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public long? PC {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public int? ProductID {get;set;}
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? Amount { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public decimal? ThisPrice {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string BarCode {get;set;}

        /// <summary>
        /// Desc:默认 = 0,已审核 = 1,已作废 = 2,已兑换 = 3,已退货=4
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? State {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public DateTime? UpdateTime {get;set;}


    }
}
