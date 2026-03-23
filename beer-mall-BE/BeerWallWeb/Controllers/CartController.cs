using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerWallWeb.Extensions;
using BeerWallWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly BeerMallContext _context;

    public CartController(BeerMallContext context)
    {
        _context = context;
    }

    // Cart 错误码（41xxx）
    private const int CODE_USER_INVALID = 41001;
    private const int CODE_PRODUCT_NOT_FOUND = 41002;
    private const int CODE_SKU_NOT_FOUND = 41003;
    private const int CODE_STOCK_NOT_ENOUGH = 41004;
    private const int CODE_PARAM_INVALID = 41006;

    // 1. 获取我的购物车
    [HttpGet]
    public async Task<ActionResult> GetCart()
    {
        var userId = User.GetUserId();
        Ensure(userId > 0, "登录状态无效", CODE_USER_INVALID, 401);

        var result = await BuildCartResult(userId);
        return Ok(result);
    }

    // 2. 加购/减购
    [HttpPost("add")]
    public async Task<ActionResult> AddToCart([FromBody] AddCartDto dto)
    {
        var userId = User.GetUserId();
        Ensure(userId > 0, "登录状态无效", CODE_USER_INVALID, 401);
        Ensure(dto != null, "参数不能为空", CODE_PARAM_INVALID);
        Ensure(dto.ProductId > 0, "productId无效", CODE_PARAM_INVALID);
        Ensure(dto.SkuId > 0, "skuId无效", CODE_PARAM_INVALID);
        Ensure(dto.Quantity != 0, "quantity不能为0", CODE_PARAM_INVALID);

        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
        Ensure(productExists, "商品不存在", CODE_PRODUCT_NOT_FOUND, 404);

        var sku = await _context.ProductSkus.FirstOrDefaultAsync(s => s.Id == dto.SkuId && s.ProductId == dto.ProductId);
        Ensure(sku != null, "SKU不存在", CODE_SKU_NOT_FOUND, 404);

        // 只有加购才校验库存
        if (dto.Quantity > 0)
        {
            Ensure(sku!.Stock >= dto.Quantity, "库存不足", CODE_STOCK_NOT_ENOUGH);
        }

        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new ShoppingCart { UserId = userId };
            _context.ShoppingCarts.Add(cart);
        }

        var item = cart.Items.FirstOrDefault(i => i.ProductSkuId == dto.SkuId);

        if (item != null)
        {
            item.Quantity += dto.Quantity;

            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
        }
        else if (dto.Quantity > 0)
        {
            cart.Items.Add(new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                ProductSkuId = dto.SkuId,
                Quantity = dto.Quantity
            });
        }

        cart.UpdatedTime = DateTime.Now;
        await _context.SaveChangesAsync();

        var result = await BuildCartResult(userId);
        return Ok(result);
    }

    // 3. 清空购物车
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart()
    {
        var userId = User.GetUserId();
        Ensure(userId > 0, "登录状态无效", CODE_USER_INVALID, 401);

        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart != null && cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    private async Task<object> BuildCartResult(long userId)
    {
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .Include(c => c.Items).ThenInclude(i => i.ProductSku)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return new { items = Array.Empty<object>(), totalCount = 0, totalPrice = 0 };
        }

        return new
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
    }

    private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
    {
        if (!condition) throw new BusinessException(message, code, httpStatus);
    }
}

public class AddCartDto
{
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public int Quantity { get; set; } // 正数加，负数减
}