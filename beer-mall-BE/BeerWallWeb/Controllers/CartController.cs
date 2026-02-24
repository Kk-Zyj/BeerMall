using BeerMall.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly BeerMallContext _context;

    public CartController(BeerMallContext context)
    {
        _context = context;
    }

    // 1. 获取我的购物车 (GET /api/cart?userId=1)
    [HttpGet]
    public async Task<ActionResult> GetCart(long userId)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items).ThenInclude(i => i.Product)     // 连表查商品图
            .Include(c => c.Items).ThenInclude(i => i.ProductSku)  // 连表查规格价
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return Ok(new { items = new object[] { }, totalCount = 0, totalPrice = 0 });

        var result = new
        {
            items = cart.Items.Select(i => new
            {
                itemId = i.Id,
                productId = i.ProductId,
                productName = i.Product.Name,
                skuId = i.ProductSkuId,
                specName = i.ProductSku.SpecName,
                price = i.ProductSku.Price,
                quantity = i.Quantity,
                image = i.Product.MainImage
            }),
            totalCount = cart.Items.Sum(i => i.Quantity),
            totalPrice = cart.Items.Sum(i => i.Quantity * i.ProductSku.Price)
        };

        return Ok(result);
    }

    // 2. 加购/减购 (POST /api/cart/add)
    [HttpPost("add")]
    public async Task<ActionResult> AddToCart([FromBody] AddCartDto dto)
    {
        // 1. 找购物车，没有就新建
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId);

        if (cart == null)
        {
            cart = new ShoppingCart { UserId = dto.UserId };
            _context.ShoppingCarts.Add(cart);
        }

        // 2. 找这个商品在这个购物车里有没有
        var item = cart.Items.FirstOrDefault(i => i.ProductSkuId == dto.SkuId);

        if (item != null)
        {
            // 已存在 -> 更新数量
            item.Quantity += dto.Quantity;
            if (item.Quantity <= 0)
            {
                // 如果减到0，直接删除
                _context.CartItems.Remove(item);
            }
        }
        else if (dto.Quantity > 0)
        {
            // 不存在 -> 新增条目
            cart.Items.Add(new CartItem
            {
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                ProductSkuId = dto.SkuId,
                Quantity = dto.Quantity
            });
        }

        cart.UpdatedTime = DateTime.Now;
        await _context.SaveChangesAsync();

        // 返回最新购物车状态，方便前端更新UI
        return await GetCart(dto.UserId);
    }

    // DELETE: api/cart/clear?userId=1
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart(long userId)
    {
        // 1. 查找该用户的购物车，并加载所有明细
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null && cart.Items.Any())
        {
            // 2. 批量删除购物车内的所有商品项
            _context.CartItems.RemoveRange(cart.Items);

            // 3. 更新时间
            cart.UpdatedTime = DateTime.Now;

            // 4. 保存数据库
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "购物车已清空" });
    }
}

public class AddCartDto
{
    public long UserId { get; set; } // 实际项目应从 Token 获取
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public int Quantity { get; set; } // 正数是加，负数是减
}