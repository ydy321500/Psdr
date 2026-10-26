using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("RedPacketRequestList")]
    public partial class RedPacketRequestListServer
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public RedPacketRequestListServer()
        {


        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long RedPackeRequestID { get; set; }
        public int ProductID { get; set; }
        public int ShopID { get; set; }

        public string AgentNo { get; set; }
        public string OpenID { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string BarCode { get; set; }

        public decimal Money { get; set; }

        public DateTime? InsertTime { get; set; }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? UpdateTime { get; set; }

    }
}
