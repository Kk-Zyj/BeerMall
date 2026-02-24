using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace BeerMall.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly BeerMallContext _context;

        public CategoryController(BeerMallContext context)
        {
            _context = context;
        }

        // GET: api/category/list
        // 获取所有启用分类 (给小程序前端用)
        [HttpGet("list")]
        public async Task<ActionResult<List<Category>>> GetList()
        {
            var list = await _context.Categories
                .Where(c => c.IsVisible)
                .OrderByDescending(c => c.Sort) // 按权重倒序，权重大的排前面
                .ThenBy(c => c.Id)
                .ToListAsync();

            return Ok(list);
        }

        // POST: api/category
        // 添加新分类 (给商家后台用)
        [HttpPost]
        public async Task<ActionResult> Create(Category category)
        {
            category.CreateTime = DateTime.Now;
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        // PUT: api/category
        // 修改分类 (给商家后台用)
        [HttpPut]
        public async Task<ActionResult> Update(Category category)
        {
            var exist = await _context.Categories.FindAsync(category.Id);
            if (exist == null) return NotFound();

            exist.Name = category.Name;
            exist.Sort = category.Sort;
            exist.IsVisible = category.IsVisible;

            await _context.SaveChangesAsync();
            return Ok(exist);
        }

        // DELETE: api/category/5
        // 删除分类
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            // 检查该分类下是否有商品，如果有，禁止删除
            var hasProduct = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProduct)
            {
                return BadRequest("该分类下包含商品，无法删除！请先转移或删除商品。");
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}