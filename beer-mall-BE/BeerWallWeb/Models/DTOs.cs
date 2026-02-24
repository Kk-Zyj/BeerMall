using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Models.DTOs
{
    // 用于列表展示（精简版）
    public class ProductListDto
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string Name { get; set; }
        public string MainImage { get; set; }
        public int Sales { get; set; }
        public decimal MinPrice { get; set; }
        public string Description { get; set; } // 列表页可能也需要简短描述
        public int SkuCount { get; set; } //规格数量
        public long DefaultSkuId { get; set; } // ✅ 新增：默认 SKU 的 ID
    }

    // 用于详情展示（完整版）
    public class ProductDetailDto : ProductListDto
    {
        public string Content { get; set; } // 富文本
        public List<SkuDto> Skus { get; set; } // 包含规格列表
    }

    //酒水规格
    public class SkuDto
    {
        public long Id { get; set; }
        public string SpecName { get; set; } // "整箱24瓶"
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public decimal Weight { get; set; } // 前端可能需要用来提示运费
    }

    //标签种类
    public class CategoryListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; } // 分类图标 (可选)
        public int Sort { get; set; } = 0; // 排序权重，数字越大越靠前 
    }

    public class CreateOrderDto
    {
        public long UserId { get; set; }
        public long AddressId { get; set; } // 用户选的地址ID (如果是自提，可能是0)
        public string DeliveryMethod { get; set; } // "express", "local", "self"  //配送方式
        public string Remark { get; set; }
        public string? ClientIp { get; set; }     // 下单时的 IP
        public string DeviceId { get; set; }     // 设备指纹 (前端生成或获取) 
        public int RiskStatus { get; set; } = 0;  // 风控状态: 0=正常, 1=疑似作弊(不计入裂变), 2=确认作弊
        public string? RiskReason { get; set; }   // 作弊理由 (如: IP重复)
        public int OrderType { get; set; } // 0=普通, 1=开团, 2=参团
        public long? GroupBuyId { get; set; } // 如果是参团，必填

        public long? UserCouponId { get; set; } // 可选的用户优惠券ID
    }

    public class RuleDto
    {
        public int RequiredPeople { get; set; } = 3; // 成团人数 (3人) 
        public decimal DiscountRate { get; set; } = 0.85m; // 折扣率 (0.85) 
        public int DurationHours { get; set; } = 24; // 有效时长 (24小时)  
    }

    public class UpdateRuleDto : RuleDto
    {
        public long ProductId { get; set; } // 关联商品ID 0 代表不限制特定商品，全场通用  
        public bool IsActive { get; set; } = true; // 开关
    }

    // 列表页展示用的 DTO
    public class AdminProductListDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string MainImage { get; set; }
        public decimal MinPrice { get; set; } // 展示最低价
        public int TotalStock { get; set; }   // 展示总库存
        public bool IsOnShelf { get; set; }
        public string CategoryName { get; set; }
        public int Sort { get; set; }
    }

    // 保存/编辑用的 DTO (包含 SKU 列表)
    public class ProductSaveDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long CategoryId { get; set; }
        public string MainImage { get; set; }
        public int Sort { get; set; }
        public bool IsOnShelf { get; set; }

        // 接收 SKU 列表
        public List<ProductSkuDto> Skus { get; set; } = new List<ProductSkuDto>();
    }

    public class ProductSkuDto
    {
        public long Id { get; set; } // 如果是新增，Id为0
        public string SpecName { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public int Stock { get; set; }
        public decimal Weight { get; set; }
    }

    public class AdminOrderListDto
    {
        public long Id { get; set; }
        public string OrderNo { get; set; }
        public string ReceiverName { get; set; } // 收货人姓名
        public string ReceiverMobile { get; set; } // 收货人电话
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public int OrderType { get; set; } // 0=普通, 1=拼团
        public string CreateTime { get; set; } // 格式化后的时间字符串
    }

    // 用于接收发货参数
    public class ShipDto
    {
        public string TrackingNo { get; set; }
    }
}