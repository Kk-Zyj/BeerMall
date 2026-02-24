using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerWallWeb.Controllers.AdminController
{
    [Route("api/app/coupon")]
    [ApiController]
    public class AppCouponController : ControllerBase
    {
        private readonly BeerMallContext _context;
        public AppCouponController(BeerMallContext context)
        {
            _context = context;
        }

        // 0. 获取当前可领取的优惠券列表
        [HttpGet("active")]
        public async Task<ActionResult> GetActiveCoupons()
        {
            var now = DateTime.Now;

            var list = await _context.Coupons
                .Where(c => c.IsActive == true) // 必须是上架状态
                .Where(c => c.ReceivedCount < c.TotalCount) // 必须还有剩余库存
                .Where(c => c.TimeType == 1 || (c.TimeType == 0 && c.EndTime > now)) // 相对时间，或者绝对时间还没过期
                .OrderByDescending(c => c.Amount) // 按面值倒序
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Amount,
                    c.MinPoint,
                    c.TimeType,
                    c.Days,
                    c.StartTime,
                    c.EndTime
                })
                .ToListAsync();

            return Ok(list);
        }

        // 1. 领券接口
        [HttpPost("{id}/receive")]
        public async Task<ActionResult> Receive(long id, [FromQuery] long userId)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null || !coupon.IsActive) return BadRequest("优惠券不存在或已下架");
            if (coupon.ReceivedCount >= coupon.TotalCount) return BadRequest("优惠券已抢光");

            // 检查用户是否领过 (假设每人限领1张，实际可配置)
            var hasReceived = await _context.UserCoupons.AnyAsync(u => u.CouponId == id && u.UserId == userId);
            if (hasReceived) return BadRequest("您已领取过该券");

            // 计算过期时间
            DateTime expireTime = coupon.TimeType == 0
                ? coupon.EndTime.Value
                : DateTime.Now.AddDays(coupon.Days);

            var userCoupon = new UserCoupon
            {
                UserId = userId,
                CouponId = id,
                Status = 0,
                ExpireTime = expireTime,
                CreateTime = DateTime.Now
            };

            coupon.ReceivedCount++; // 增加已领数量
            _context.UserCoupons.Add(userCoupon);
            await _context.SaveChangesAsync();

            return Ok("领取成功");
        }

        // 2. 查看“我的可用优惠券” (用于下单页展示)
        [HttpGet("my/available")]
        public async Task<ActionResult> GetMyAvailableCoupons(long userId, decimal orderAmount)
        {
            // 逻辑：未使用的 + 没过期的 + 满足金额门槛的
            var list = await _context.UserCoupons
                .Include(u => u.Coupon)
                .Where(u => u.UserId == userId && u.Status == 0 && u.ExpireTime > DateTime.Now)
                .Where(u => u.Coupon.MinPoint <= orderAmount)
                .Select(u => new
                {
                    u.Id, // UserCouponId
                    u.Coupon.Name,
                    u.Coupon.Amount,
                    u.Coupon.MinPoint,
                    u.ExpireTime
                })
                .OrderByDescending(x => x.Amount) // 优惠金额大的排前面
                .ToListAsync();

            return Ok(list);
        }
    }
}
