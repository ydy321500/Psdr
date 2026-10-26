using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("NewMessage")]
    public partial class NewMessage
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public NewMessage(){


        }
        /// <summary>
        /// 主键ID(标准)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public Int64 ID
        {
            get;
            set;
        }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        public int ToObject { get;set;}

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Title { get;set;}

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Context { get;set;}
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
        public DateTime Time { get;set;}

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string ReadUser { get;set;}

    }
}
