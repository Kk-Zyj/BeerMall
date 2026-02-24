using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace BeerMall.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly BeerMallContext _context;

        public AddressController(BeerMallContext context)
        {
            _context = context;
        }

        // 1. 获取列表
        [HttpGet]
        public async Task<ActionResult> GetList(long userId)
        {
            var list = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .ToListAsync();
            return Ok(list);
        }

        // 2. 获取单个详情 (用于编辑回显)
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(long id)
        {
            var address = await _context.UserAddresses.FindAsync(id);
            if (address == null) return NotFound();
            return Ok(address);
        }

        // 3. 新增/保存
        [HttpPost]
        public async Task<ActionResult> Save([FromBody] UserAddress dto)
        {
            // 如果设为默认，先把其他的取消默认
            if (dto.IsDefault)
            {
                var defaults = await _context.UserAddresses
                    .Where(a => a.UserId == dto.UserId && a.IsDefault)
                    .ToListAsync();
                defaults.ForEach(a => a.IsDefault = false);
            }

            if (dto.Id > 0)
            {
                // 更新
                _context.UserAddresses.Update(dto);
            }
            else
            {
                // 新增
                _context.UserAddresses.Add(dto);
            }

            await _context.SaveChangesAsync();
            return Ok(dto);
        }

        // 4. 删除
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var address = await _context.UserAddresses.FindAsync(id);
            if (address != null)
            {
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
            }
            return Ok();
        }
    }
}