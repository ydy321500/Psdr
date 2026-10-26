using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Psdr.Models.Entities
{
    [SugarTable("RedPacketList")]
    public partial class RedPacketList
    {
        /// <summary>
        /// 初始化。
        /// </summary>
        public RedPacketList()
        {
            //红包表，出售后就会在此表中有记录
            //退货后则删除
        }
        /// <summary>
        /// Desc:
        /// Default:
        /// Nullable:False
        /// </summary>           
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long RedPacketID { get; set; }

        public int ProductID { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(ProductID), nameof(Product.ProductID))]
        public Product product { get; set; }
        public int ShopID { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(ShopID), nameof(Shop.ShopID))]
        public Shop shop { get; set; }

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
        public DateTime? UpdateTime { get; set; }
    }
}
