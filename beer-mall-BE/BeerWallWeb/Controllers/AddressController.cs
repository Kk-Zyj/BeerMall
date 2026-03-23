using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerWallWeb.Extensions;
using BeerWallWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerMall.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AddressController : ControllerBase
    {
        private readonly BeerMallContext _context;

        private const int CODE_UNAUTHORIZED = 40001;
        private const int CODE_ADDRESS_NOT_FOUND = 40002;
        private const int CODE_PARAM_INVALID = 40003;

        public AddressController(BeerMallContext context)
        {
            _context = context;
        }

        // 1. 获取当前用户地址列表
        [HttpGet]
        public async Task<ActionResult> GetList()
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var list = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            return Ok(list);
        }

        // 2. 获取当前用户单个地址详情
        [HttpGet("{id}")]
        public async Task<ActionResult> Get(long id)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            Ensure(address != null, "地址不存在", CODE_ADDRESS_NOT_FOUND, 404);
            return Ok(address);
        }

        // 3. 新增 / 保存
        [HttpPost]
        public async Task<ActionResult> Save([FromBody] UserAddress dto)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);
            Ensure(dto != null, "参数不能为空", CODE_PARAM_INVALID);

            Ensure(!string.IsNullOrWhiteSpace(dto.Name), "联系人不能为空", CODE_PARAM_INVALID);
            Ensure(!string.IsNullOrWhiteSpace(dto.Mobile), "手机号不能为空", CODE_PARAM_INVALID);
            Ensure(!string.IsNullOrWhiteSpace(dto.Province), "省不能为空", CODE_PARAM_INVALID);
            //Ensure(!string.IsNullOrWhiteSpace(dto.City), "市不能为空", CODE_PARAM_INVALID);
            Ensure(!string.IsNullOrWhiteSpace(dto.District), "区/县不能为空", CODE_PARAM_INVALID);
            Ensure(!string.IsNullOrWhiteSpace(dto.Detail), "详细地址不能为空", CODE_PARAM_INVALID);

            // 如果设为默认，先把当前用户其他默认地址取消
            if (dto.IsDefault)
            {
                var defaults = await _context.UserAddresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();

                defaults.ForEach(a => a.IsDefault = false);
            }

            if (dto.Id > 0)
            {
                var entity = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.Id == dto.Id && a.UserId == userId);

                Ensure(entity != null, "地址不存在", CODE_ADDRESS_NOT_FOUND, 404);

                entity.Name = dto.Name;
                entity.Mobile = dto.Mobile;
                entity.Province = dto.Province;
                entity.City = dto.City;
                entity.District = dto.District;
                entity.Detail = dto.Detail;
                entity.IsDefault = dto.IsDefault;

                await _context.SaveChangesAsync();
                return Ok(entity);
            }
            else
            {
                var entity = new UserAddress
                {
                    UserId = userId,
                    Name = dto.Name,
                    Mobile = dto.Mobile,
                    Province = dto.Province,
                    City = dto.City,
                    District = dto.District,
                    Detail = dto.Detail,
                    IsDefault = dto.IsDefault
                };

                _context.UserAddresses.Add(entity);
                await _context.SaveChangesAsync();
                return Ok(entity);
            }
        }

        // 4. 删除
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(long id)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var address = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            Ensure(address != null, "地址不存在", CODE_ADDRESS_NOT_FOUND, 404);

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
        {
            if (!condition) throw new BusinessException(message, code, httpStatus);
        }
    }
}