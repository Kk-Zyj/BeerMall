using BeerWallWeb.API.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Entities
{
    public class FissionTask : BaseEntity
    {
        public long InitiatorId { get; set; } // 发起人ID
        public long OrderId { get; set; }     // 源订单ID

        [Column(TypeName = "decimal(18,2)")]
        public decimal SourceOrderAmount { get; set; } // 源订单金额

        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetThreshold { get; set; }   // 助力门槛 (源金额 - 5)

        public int TargetCount { get; set; } = 3;      // 目标人数
        public int CurrentCount { get; set; } = 0;     // 当前人数

        // 记录助力者的ID列表，格式: "[101,102,103]"
        public string ParticipantLog { get; set; } = "[]";

        // 状态: 0=进行中, 1=成功待领, 2=已返现, -1=失败/过期
        public int Status { get; set; }

        public DateTime ExpireTime { get; set; } // 过期时间
    } 
}