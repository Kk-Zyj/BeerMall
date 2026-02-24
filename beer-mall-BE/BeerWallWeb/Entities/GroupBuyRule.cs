using BeerWallWeb.API.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Entities
{
    public class GroupBuyRule : BaseEntity
    {
        public long ProductId { get; set; } // 关联商品ID 0 代表不限制特定商品，全场通用

        public int RequiredPeople { get; set; } = 3; // 成团人数 (3人)

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountRate { get; set; } = 0.85m; // 折扣率 (0.85)

        public int DurationHours { get; set; } = 24; // 有效时长 (24小时)

        public bool IsActive { get; set; } = true; // 开关
    }
}
