using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BeerMall.Api.Data;
using BeerMall.Api.Models.DTOs;
using BeerMall.Api.Entities;

namespace BeerMall.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly BeerMallContext _context;

        public ProductController(BeerMallContext context)
        {
            _context = context;
        }

        // GET: api/product/list
        [HttpGet("list")]
        public async Task<ActionResult<List<ProductListDto>>> GetList()
        {
            var products = await _context.Products
                 .Include(p => p.Skus)
                 .Where(p => p.IsOnShelf)
                 .OrderByDescending(p => p.Sort)
                 .ToListAsync();

            var dtos = products.Select(p => new ProductListDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId, // 👈 这一行是关键！
                Name = p.Name,
                MainImage = p.MainImage,
                Sales = p.Sort,
                Description = p.Description, // 加上描述
                SkuCount = p.Skus.Count,//  统计该商品下的 SKU 数量
                MinPrice = p.Skus.Any() ? p.Skus.Min(s => s.Price) : 0,
                DefaultSkuId = p.Skus.Any() ? p.Skus.FirstOrDefault().Id : 0
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/product/1
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDetailDto>> GetDetail(long id)
        {
            var product = await _context.Products
                .Include(p => p.Skus)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound("商品不存在");

            var dto = new ProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                MainImage = product.MainImage,
                Description = product.Description,
                Content = product.Content,
                Skus = product.Skus.Select(s => new SkuDto
                {
                    Id = s.Id,
                    SpecName = s.SpecName,
                    Price = s.Price,
                    Stock = s.Stock,
                    Weight = s.Weight
                }).ToList()
            };

            return Ok(dto);
        }
    }
}