using BeerMall.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeerMall.Api.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    public class AdminDashboardController : ControllerBase
    {
        private readonly BeerMallContext _context;

        public AdminDashboardController(BeerMallContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary()
        {
            var today = DateTime.Today;

            // 1. 今日数据 (只统计已付款及以上的订单: Status >= 1, 或者 Status == 10)
            // 注意：你的系统中 10 是待成团，1 是待发货，2 是已发货，3 是完成。这些都算有效营收。
            var todayOrders = await _context.Orders
                .Where(o => o.CreateTime >= today && (o.Status >= 1 || o.Status == 10))
                .ToListAsync();

            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
            var todayCount = todayOrders.Count;

            // 2. 待处理事项 (待发货 Status = 1)
            var pendingShipmentCount = await _context.Orders
                .CountAsync(o => o.Status == 1);

            // 3. 累计总营收
            var totalRevenue = await _context.Orders
                .Where(o => o.Status >= 1 || o.Status == 10)
                .SumAsync(o => o.TotalAmount);

            return Ok(new
            {
                todayRevenue,
                todayCount,
                pendingShipmentCount,
                totalRevenue
            });
        }

        [HttpGet("chart")]
        public async Task<ActionResult> GetChartData()
        {
            var sevenDaysAgo = DateTime.Today.AddDays(-6); // 包含今天共7天

            // 查出最近7天的有效订单
            var orders = await _context.Orders
                 .Where(o => o.CreateTime >= sevenDaysAgo && (o.Status >= 1 || o.Status == 10))
                .Select(o => new { o.CreateTime, o.TotalAmount })
                .ToListAsync();

            // 按天分组统计
            var groupedData = orders
                .GroupBy(o => o.CreateTime.Date)
                .ToDictionary(g => g.Key, g => g.Sum(o => o.TotalAmount));

            var dates = new List<string>();
            var amounts = new List<decimal>();

            // 补全7天的数据（哪怕某天是0）
            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                dates.Add(date.ToString("MM-dd"));
                amounts.Add(groupedData.ContainsKey(date) ? groupedData[date] : 0);
            }

            return Ok(new
            {
                dates,
                amounts
            });
        }
    }
}