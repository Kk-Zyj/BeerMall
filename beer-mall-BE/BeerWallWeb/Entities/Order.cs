using BeerWallWeb.API.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Entities
{
    public enum OrderStatus
    {
        PendingPayment = 0, // 待支付
        PendingDelivery = 1, // 待发货
        Shipped = 2,        // 已发货
        Completed = 3,      // 已完成
        Cancelled = -1      // 已取消
    }
    public class Order
    {
        public long Id { get; set; }
        public string OrderNo { get; set; } // 订单号 (例如: 202601250001)
        public long UserId { get; set; }

        // 金额相关
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // 最终实付总价

        [Column(TypeName = "decimal(18,2)")]
        public decimal ProductAmount { get; set; } // 商品总价

        [Column(TypeName = "decimal(18,2)")]
        public decimal FreightAmount { get; set; } // 运费
       
        public decimal CouponAmount { get; set; } // 优惠券抵扣金额

        // 订单状态: 0=待付款, 1=待发货, 2=待收货, 3=已完成, -1=已取消
        public int Status { get; set; } = 0;

        // 配送与地址快照  避免用户修改地址后，旧订单的地址发生变化
        public string DeliveryMethod { get; set; } // express, local, self
        public string ReceiverName { get; set; }
        public string ReceiverMobile { get; set; }
        public string ReceiverAddress { get; set; } // 省市区+详细地址拼在一起

        public string Remark { get; set; } // 买家留言
        public DateTime CreateTime { get; set; } = DateTime.Now;

        // 导航属性
        public List<OrderItem> Items { get; set; }

        // 风控字段
        public string? ClientIp { get; set; }     // 下单时的 IP
        public string DeviceId { get; set; }     // 设备指纹 (前端生成或获取)

        /// <summary>
        /// 关联的父级裂变任务ID (我是助力者，我帮这个任务砍了一刀)
        /// </summary>
        public long? ParentTaskId { get; set; }

        // 风控状态: 0=正常, 1=疑似作弊(不计入裂变), 2=确认作弊
        public int RiskStatus { get; set; } = 0;
        public string? RiskReason { get; set; }   // 作弊理由 (如: IP重复)
        public long? GroupBuyId { get; set; } // 关联的拼团实例ID
        public int OrderType { get; set; } = 0; // 0=普通, 1=拼团
    }

    public class OrderItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public long SkuId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public string SpecName { get; set; } // 规格名称

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // 下单时的单价
        public int Quantity { get; set; }
    }
}