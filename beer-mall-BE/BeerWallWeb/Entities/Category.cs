// Entities/Category.cs
using System.ComponentModel.DataAnnotations;

namespace BeerMall.Api.Entities
{
    public class Category
    {
        public long Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } // 分类名称，如 "精酿啤酒"

        public int Sort { get; set; } = 0; // 排序权重，数字越大越靠前

        [MaxLength(200)]
        public string? Icon { get; set; } // 分类图标 (可选)

        public bool IsVisible { get; set; } = true; // 是否启用

        public DateTime CreateTime { get; set; } = DateTime.Now;

        // 导航属性：一个分类下有多个商品
        public List<Product> Products { get; set; }
    }
}