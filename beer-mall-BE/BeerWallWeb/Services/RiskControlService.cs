using BeerMall.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BeerWallWeb.Services
{
    public class RiskControlService
    {
        private readonly BeerMallContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RiskControlService(BeerMallContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Pass, string Reason)> CheckRiskAsync(long userId, string deviceId)
        {
            string ip = GetClientIp();

            // 1. 同机风控：同一设备24小时内超过2个账号下单
            if (!string.IsNullOrEmpty(deviceId))
            {
                int deviceUserCount = await _context.Orders
                    .Where(o => o.DeviceId == deviceId && o.CreateTime > DateTime.Now.AddHours(-24))
                    .Select(o => o.UserId)
                    .Distinct()
                    .CountAsync();

                if (deviceUserCount >= 2)
                {
                    // 如果该用户以前用过这台设备，则放行；否则拦截
                    bool isOldUser = await _context.Orders.AnyAsync(o => o.DeviceId == deviceId && o.UserId == userId);
                    if (!isOldUser) return (false, "设备关联过多账号");
                }
            }

            // 2. IP风控：同一IP 24小时内超过20单
            int ipCount = await _context.Orders
                .CountAsync(o => o.ClientIp == ip && o.CreateTime > DateTime.Now.AddHours(-24));

            if (ipCount > 20) return (false, "IP请求过于频繁");

            return (true, "");
        }

        public string GetClientIp()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
                return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0];
            return context?.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }
    }
}