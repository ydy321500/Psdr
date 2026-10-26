using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("ReturnInfo")]
    public partial class ReturnInfo
    {
           /// <summary>
           /// 初始化。
           /// </summary>
           public ReturnInfo(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true)]
           public long ReturnInfoID {get;set;}

            /// <summary>
            /// Desc:
            /// Default:
            /// Nullable:True
            /// </summary>           
           public string No {get;set;}

            [Navigate(NavigateType.OneToOne, nameof(No), nameof(Return.No))]
            public Return returns { get; set; }
            
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
        [Navigate(NavigateType.OneToOne, nameof(ProductID), nameof(Product.ProductID))]
        public Product product { get; set; }
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
           /// Desc:
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
