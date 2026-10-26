using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("RedPacketRequestList")]
    public class RedPacketRequestList
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public RedPacketRequestList()
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
        [Navigate(NavigateType.OneToOne, nameof(ProductID), nameof(Product.ProductID))]
        public Product product { get; set; }
        public int ShopID { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(ShopID), nameof(Shop.ShopID))]
        public Shop shop { get; set; }

        public string AgentNo { get; set; }
        
        public string OpenID { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(OpenID), nameof(SalesPerson.OpenID))]
        public SalesPerson salesperson { get; set; }
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
