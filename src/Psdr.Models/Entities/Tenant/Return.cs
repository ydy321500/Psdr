using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("Return")]
    public partial class Return
    {
           /// <summary>
           /// 初始化。
           /// </summary>
           public Return(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsIdentity=true)]
           public long ReturnID {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true)]
           public string No {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public int? ShopID {get;set;}

           /// <summary>
           /// 客户退货回库的仓库编号。
           /// </summary>
           public int? StoreHouseID { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(ShopID), nameof(Shop.ShopID))]
        public Shop shop { get; set; } //不能赋值只能是null
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? Time {get;set;}
        public string ProductName { get; set; }
        public int? Amount { get; set; }
        public decimal? TotalMoney { get; set; }
        public decimal? OtherMoney { get; set; }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Remark {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public int? State {get;set;}

    }
}
