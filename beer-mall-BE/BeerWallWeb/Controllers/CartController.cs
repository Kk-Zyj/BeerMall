using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerWallWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    // 1. 获取我的购物车 (GET /api/Cart?userId=1)
    [HttpGet]
    public async Task<ActionResult> GetCart([FromQuery] long userId)
    {
        Ensure(userId > 0, "userId无效", CODE_USER_INVALID);

        // 影子用户也应该存在；如果你允许“userId 不存在也自动创建购物车”，那就改成不校验
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        Ensure(userExists, "用户不存在", CODE_USER_INVALID, 404);

        var cart = await _context.ShoppingCarts
            .Include(c => c.Items).ThenInclude(i => i.Product)
            .Include(c => c.Items).ThenInclude(i => i.ProductSku)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
            return Ok(new { items = Array.Empty<object>(), totalCount = 0, totalPrice = 0 });

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

    // 2. 加购/减购 (POST /api/Cart/add)
    [HttpPost("add")]
    public async Task<ActionResult> AddToCart([FromBody] AddCartDto dto)
    {
        Ensure(dto != null, "参数不能为空", CODE_PARAM_INVALID);
        Ensure(dto.UserId > 0, "userId无效", CODE_USER_INVALID);
        Ensure(dto.ProductId > 0, "productId无效", CODE_PARAM_INVALID);
        Ensure(dto.SkuId > 0, "skuId无效", CODE_PARAM_INVALID);
        Ensure(dto.Quantity != 0, "quantity不能为0", CODE_PARAM_INVALID);

        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        Ensure(userExists, "用户不存在", CODE_USER_INVALID, 404);

        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
        Ensure(productExists, "商品不存在", CODE_PRODUCT_NOT_FOUND, 404);

        var sku = await _context.ProductSkus.FirstOrDefaultAsync(s => s.Id == dto.SkuId && s.ProductId == dto.ProductId);
        Ensure(sku != null, "SKU不存在", CODE_SKU_NOT_FOUND, 404);

        // 只有加购才校验库存；减购允许（减到0会删除）
        if (dto.Quantity > 0)
        {
            Ensure(sku!.Stock >= dto.Quantity, "库存不足", CODE_STOCK_NOT_ENOUGH);
        }

        // 找购物车，没有就新建
        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == dto.UserId);

        if (cart == null)
        {
            cart = new ShoppingCart { UserId = dto.UserId };
            _context.ShoppingCarts.Add(cart);
        }

        // 找条目
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
                UserId = dto.UserId,
                ProductId = dto.ProductId,
                ProductSkuId = dto.SkuId,
                Quantity = dto.Quantity
            });
        }
        // else：dto.Quantity < 0 且 item 不存在 -> 不做任何事（幂等）

        cart.UpdatedTime = DateTime.Now;
        await _context.SaveChangesAsync();

        // 返回最新购物车状态（你原来就是这样做的）
        return await GetCart(dto.UserId);
    }

    // DELETE: /api/Cart/clear?userId=1
    [HttpDelete("clear")]
    public async Task<ActionResult> ClearCart([FromQuery] long userId)
    {
        Ensure(userId > 0, "userId无效", CODE_USER_INVALID);

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        Ensure(userExists, "用户不存在", CODE_USER_INVALID, 404);

        var cart = await _context.ShoppingCarts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        // 幂等：没有购物车或已空 -> 也当成功
        if (cart != null && cart.Items.Any())
        {
            _context.CartItems.RemoveRange(cart.Items);
            cart.UpdatedTime = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
    {
        if (!condition) throw new BusinessException(message, code, httpStatus);
    }
}

public class AddCartDto
{
    public long UserId { get; set; }     // 实际项目应从 Token 获取（后续第5步再改）
    public long ProductId { get; set; }
    public long SkuId { get; set; }
    public int Quantity { get; set; }    // 正数是加，负数是减
}