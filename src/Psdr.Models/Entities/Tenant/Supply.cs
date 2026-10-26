using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("Supply")]
    public partial class Supply
    {
           /// <summary>
           /// 初始化。
           /// </summary>
           public Supply(){


           }
           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:False
           /// </summary>           
           [SugarColumn(IsPrimaryKey=true,IsIdentity=true)]
           public int SupplyID {get;set;}
            /// <summary>
            /// Desc:
            /// Default:
            /// Nullable:True
            /// </summary>           
            public string SupplyName { get; set; }
            /// <summary>
            /// Desc:
            /// Default:
            /// Nullable:True
            /// </summary>           
            public string Province {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string City {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string Addr {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string Tel {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string Phone {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string Fax {get;set;}

           /// <summary>
           /// Desc:
           /// Default:
           /// Nullable:True
           /// </summary>           
           public string Contacts {get;set;}
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Remark { get; set; }

    }
}
