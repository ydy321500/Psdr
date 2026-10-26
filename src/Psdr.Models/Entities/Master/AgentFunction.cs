using System;
using System.Linq;
using System.Text;
using SqlSugar;

namespace Psdr.Models.Entities
{
    ///<summary>
    ///
    ///</summary>
    [SugarTable("AgentFunction")]
    public partial class AgentFunction
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public AgentFunction(){


        }
        /// <summary>
        /// 主键ID(标准)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long FunctionID
        {
            get;
            set;
        }
        /// <summary>
        /// 
        /// </summary>
        public Guid AgentID
        {
            get;
            set;
        }

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string FunctionName { get;set;}

        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime StopTime { get;set;}
       
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int FunctionType { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public int State { get; set; }

    }
}
