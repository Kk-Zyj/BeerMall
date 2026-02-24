// Entities/ShoppingCart.cs
using BeerMall.Api.Entities;
using BeerWallWeb.API.Entities;
using System.ComponentModel.DataAnnotations.Schema;

public class ShoppingCart
{
    public long Id { get; set; }

    public long UserId { get; set; } // 绑定用户

    // 购物车里的明细
    public List<CartItem> Items { get; set; } = new List<CartItem>();

    public DateTime UpdatedTime { get; set; } = DateTime.Now;
}

// Entities/CartItem.cs
public class CartItem : BaseEntity
{
    public long UserId { get; set; }

    public long ProductId { get; set; }

    // 🔥 新增：关联具体的 SKU
    public long ProductSkuId { get; set; }

    public int Quantity { get; set; }

    // 导航属性
    public virtual Product Product { get; set; }

    // 导航属性，方便取价格和库存
    [ForeignKey("ProductSkuId")]
    public virtual ProductSku ProductSku { get; set; }
}