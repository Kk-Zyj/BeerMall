using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace BeerWallWeb.Controllers.AdminController
{
    [Authorize(Roles = "Admin")]
    [Route("api/admin/coupon")]
    public class AdminCouponController : ControllerBase
    {
        private readonly BeerMallContext _context;
        public AdminCouponController(BeerMallContext context)
        {
            _context = context;
        }

        // 1. 创建优惠券接口
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Coupon coupon)
        {
            coupon.CreateTime = DateTime.Now;
            coupon.ReceivedCount = 0;
            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return Ok(coupon);
        }

        // 2. 优惠券列表接口 (用于后台管理)
        [HttpGet("list")]
        public async Task<ActionResult> GetList()
        {
            var list = await _context.Coupons.OrderByDescending(c => c.Id).ToListAsync();
            return Ok(list);
        }
    }
}
