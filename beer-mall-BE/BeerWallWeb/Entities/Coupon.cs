using BeerWallWeb.API.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Entities
{
    public class Coupon : BaseEntity
    {
        public string Name { get; set; } // 名称：如“新春大促券”
        public decimal Amount { get; set; } // 面值：10元
        public decimal MinPoint { get; set; } // 门槛：满100元可用
        public int TotalCount { get; set; } // 发行总量
        public int ReceivedCount { get; set; } // 已领取数量

        // 有效期类型：0=固定日期(2023-10-01到10-07)，1=领券后N天有效
        public int TimeType { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int Days { get; set; } // 领券后多少天有效

        public bool IsActive { get; set; } // 是否上架
    }

    public class UserCoupon : BaseEntity
    {
        public long UserId { get; set; }
        public long CouponId { get; set; }

        [ForeignKey("CouponId")]
        public Coupon Coupon { get; set; }

        public int Status { get; set; } // 0=未使用, 1=已使用, 2=已过期
        public DateTime? UsedTime { get; set; }
        public long? OrderId { get; set; } // 关联的订单ID

        // 冗余具体的过期时间，方便查询
        public DateTime ExpireTime { get; set; }
    }
}
