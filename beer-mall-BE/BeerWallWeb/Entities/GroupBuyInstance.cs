using BeerWallWeb.API.Entities;

namespace BeerMall.Api.Entities
{
    // Entities/GroupBuyInstance.cs
    public class GroupBuyInstance : BaseEntity
    {
        public long RuleId { get; set; }      // 关联哪个规则
        public long InitiatorId { get; set; } // 团长用户ID

        public string GroupNo { get; set; }   // 拼团编号 (用于分享)

        public int CurrentCount { get; set; } = 0; // 当前人数
        public int TargetCount { get; set; }       // 目标人数 (冗余存一份，防止规则变更)

        public DateTime StartTime { get; set; }
        public DateTime ExpireTime { get; set; }   // 过期时间 = Start + Duration

        // 状态: 0=拼团中, 1=拼团成功(待发货), -1=拼团失败(已退款)
        public int Status { get; set; }
    }
}
