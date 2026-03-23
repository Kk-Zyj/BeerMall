using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerMall.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using static SKIT.FlurlHttpClient.Wechat.Api.Models.ProductSPUGetListResponse.Types;
using Microsoft.AspNetCore.Authorization;

namespace BeerWallWeb.Controllers.AdminController
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly BeerMallContext _context;
        public AdminController(BeerMallContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet("{productId}")]
        public async Task<ActionResult<RuleDto>> GetRule(long productId)
        {
            var rule = await _context.GroupBuyRules.FirstOrDefaultAsync(p => p.ProductId == productId);

            if (rule == null)
            {
                return Ok(
                new RuleDto
                {
                    DiscountRate = 0.85m,
                    DurationHours = 24,
                    RequiredPeople = 3
                });
            }

            var dto = new RuleDto
            {
                DiscountRate = rule.DiscountRate,
                DurationHours = rule.DurationHours,
                RequiredPeople = rule.RequiredPeople
            };

            return Ok(dto);
        }

        [HttpPost("group-rule/update")]
        public async Task<ActionResult> UpdateGlobalRule([FromBody] UpdateRuleDto dto)
        {
            // 查找全场规则
            var rule = await _context.GroupBuyRules.FirstOrDefaultAsync(r => r.ProductId == 0);

            if (rule == null)
            {
                rule = new GroupBuyRule { ProductId = 0 };
                _context.GroupBuyRules.Add(rule);
            }

            // 🔥 商家在这里修改参数
            rule.RequiredPeople = dto.RequiredPeople; // 修改成团人数 (如: 5人)
            rule.DiscountRate = dto.DiscountRate;     // 修改折扣 (如: 0.80)
            rule.DurationHours = dto.DurationHours;   // 修改时效 (如: 48小时)
            rule.IsActive = dto.IsActive;             // 开关

            await _context.SaveChangesAsync();
            return Ok("规则已更新");
        }

        // 1. 获取商品列表(SPU维度)
        [HttpGet("product/list")]
        public async Task<ActionResult> GetList([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Products
                .Include(p => p.Skus)      // 关联 SKU 获取价格
                .Include(p => p.Category)  // 关联分类
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.Name.Contains(keyword));
            }

            var total = await query.CountAsync();

            var list = await query
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    MainImage = p.MainImage,
                    IsOnShelf = p.IsOnShelf,
                    CategoryName = p.Category.Name,
                    Sort = p.Sort,
                    // 计算该商品下所有规格的最低价
                    MinPrice = p.Skus.Any() ? p.Skus.Min(s => s.Price) : 0,
                    // 计算总库存
                    TotalStock = p.Skus.Sum(s => s.Stock)
                })
                .ToListAsync();

            return Ok(new { total, list });
        }

        // 2. 获取单个详情 (用于编辑回显)
        [HttpGet("product/{id}")]
        public async Task<ActionResult> GetDetail(long id)
        {
            var product = await _context.Products
                .Include(p => p.Skus)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            // 转成 DTO 返回给前端
            var dto = new ProductSaveDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                MainImage = product.MainImage,
                Sort = product.Sort,
                IsOnShelf = product.IsOnShelf,
                Skus = product.Skus.Select(s => new ProductSkuDto
                {
                    Id = s.Id,
                    SpecName = s.SpecName,
                    Price = s.Price,
                    OriginalPrice = s.OriginalPrice,
                    Stock = s.Stock,
                    Weight = s.Weight
                }).ToList()
            };

            return Ok(dto);
        }

        // 3. 上下架操作
        [HttpPut("product/{id}/shelf")]
        public async Task<ActionResult> SetShelf(long id, [FromBody] bool isOnShelf)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsOnShelf = isOnShelf;
            await _context.SaveChangesAsync();
            return Ok();
        }

        // 4. 保存商品 (新增或更新)
        [HttpPost("product/save")]
        public async Task<ActionResult> Save([FromBody] ProductSaveDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Product product;

                // A. 新增逻辑
                if (dto.Id == 0)
                {
                    product = new Product
                    {
                        Name = dto.Name,
                        Description = dto.Description,
                        Content ="",
                        CategoryId = dto.CategoryId,
                        MainImage = dto.MainImage,
                        Sort = dto.Sort,
                        IsOnShelf = dto.IsOnShelf,
                        CreateTime = DateTime.Now
                    };
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync(); // 先保存获取 ProductId
                }
                // B. 更新逻辑
                else
                {
                    product = await _context.Products
                        .Include(p => p.Skus)
                        .FirstOrDefaultAsync(p => p.Id == dto.Id);

                    if (product == null) return NotFound();

                    product.Name = dto.Name;
                    product.Description = dto.Description;
                    product.CategoryId = dto.CategoryId;
                    product.MainImage = dto.MainImage;
                    product.Sort = dto.Sort;
                    product.IsOnShelf = dto.IsOnShelf;
                    product.UpdateTime = DateTime.Now;

                    // --- 处理 SKU 的更新/删除 ---
                    // 1. 找出数据库里有，但前端没传的 SKU -> 删除
                    var incomingSkuIds = dto.Skus.Where(s => s.Id > 0).Select(s => s.Id).ToList();
                    var skusToDelete = product.Skus.Where(s => !incomingSkuIds.Contains(s.Id)).ToList();
                    _context.ProductSkus.RemoveRange(skusToDelete);
                }

                // --- 处理 SKU 的新增/修改 ---
                foreach (var skuDto in dto.Skus)
                {
                    if (skuDto.Id == 0)
                    {
                        // 新增 SKU
                        var newSku = new ProductSku
                        {
                            ProductId = product.Id, // 关联 ID
                            SpecName = skuDto.SpecName,
                            Price = skuDto.Price,
                            OriginalPrice = skuDto.OriginalPrice,
                            Stock = skuDto.Stock,
                            Weight = skuDto.Weight,
                            CreateTime = DateTime.Now
                        };
                        _context.ProductSkus.Add(newSku);
                    }
                    else
                    {
                        // 修改现有 SKU
                        var existingSku = product.Skus.FirstOrDefault(s => s.Id == skuDto.Id);
                        if (existingSku != null)
                        {
                            existingSku.SpecName = skuDto.SpecName;
                            existingSku.Price = skuDto.Price;
                            existingSku.OriginalPrice = skuDto.OriginalPrice;
                            existingSku.Stock = skuDto.Stock;
                            existingSku.Weight = skuDto.Weight;
                            existingSku.UpdateTime = DateTime.Now;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest("保存失败：" + ex.Message);
            }
        }

        // 1. 获取订单列表 (分页 + 筛选)
        [HttpGet("order/list")]
        public async Task<ActionResult> GetList(
            [FromQuery] int status = -99,      // -99 代表全部
            [FromQuery] string? keyword = null,// 搜索关键词
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Orders.AsQueryable();

            // --- A. 状态筛选 ---
            if (status != -99)
            {
                query = query.Where(o => o.Status == status);
            }

            // --- B. 关键词搜索 (订单号 或 收货人) ---
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o => o.OrderNo.Contains(keyword) || o.ReceiverName.Contains(keyword));
            }

            // --- C. 获取总数 ---
            var total = await query.CountAsync();

            // --- D. 分页 & 映射 DTO ---
            var list = await query
                .OrderByDescending(o => o.CreateTime) // 最新订单排前面
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderListDto
                {
                    Id = o.Id,
                    OrderNo = o.OrderNo,
                    ReceiverName = o.ReceiverName,
                    ReceiverMobile = o.ReceiverMobile,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    OrderType = o.OrderType,
                    // 直接在数据库查询时格式化时间 (SQL Server适用，若是其他DB可能需查出来再Format)
                    CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return Ok(new { total, list });
        }

        // 2. 发货接口 (复习)
        [HttpPost("order/{id}/ship")]
        public async Task<ActionResult> ShipOrder(long id, [FromBody] ShipDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("订单不存在");

            if (order.Status != 1) return BadRequest("当前状态不可发货");

            order.Status = 2; // 已发货
            // order.TrackingNo = dto.TrackingNo; // 如果有物流单号字段 

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }

}
