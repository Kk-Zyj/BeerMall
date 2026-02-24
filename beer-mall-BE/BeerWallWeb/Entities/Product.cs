using BeerWallWeb.API.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeerMall.Api.Entities
{
    // SPU：标准商品单元（如：百威啤酒）
    public class Product : BaseEntity
    {
        public string Name { get; set; } // 商品名称
        public string Description { get; set; } // 商品简介
        public string Content { get; set; } // 富文本详情
        
        public long CategoryId { get; set; }

        // 2. 添加导航属性 (Virtual 是为了 EF Core 的延迟加载，也可以不加)
        // [ForeignKey("CategoryId")] // 如果命名规范，EF Core 会自动识别，不加这个特性也可以
        public virtual Category Category { get; set; }

        public string MainImage { get; set; } // 主图
        public int Sort { get; set; } // 排序
        public bool IsOnShelf { get; set; } // 是否上架

        // 导航属性：一个商品对应多个规格
        public List<ProductSku> Skus { get; set; } = new List<ProductSku>();
    }

    // SKU：库存量单位（如：百威啤酒-整箱24瓶）
    public class ProductSku : BaseEntity
    {
        public long ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        public string SpecName { get; set; } // 规格名称： "单瓶500ml" 或 "整箱24瓶"

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // 售价

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPrice { get; set; } // 原价（划线价）

        public int Stock { get; set; } // 库存

        // 啤酒电商关键字段：重量（kg），用于计算运费
        [Column(TypeName = "decimal(10,2)")]
        public decimal Weight { get; set; }
    }
}