using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("Indent")]
    public partial class Indent
    {
           /// <summary>
           /// 初始化。
           /// </summary>
           public Indent(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsIdentity=true)]
           public long OrderID {get;set;}

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
           /// 销售出库的仓库编号。
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
        public string Remark {get;set;}

        /// <summary>
        /// Desc:默认 = 0,已审核 = 1,已作废 = 2,已兑换 = 3,已退货=4
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int? State {get;set;}

    }
}
