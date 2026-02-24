using BeerMall.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerMall.Api.Services
{
    /// <summary>
    /// 订单超时自动回滚库存
    /// </summary>
    public class OrderTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OrderTimeoutService> _logger;

        public OrderTimeoutService(IServiceProvider services, ILogger<OrderTimeoutService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<BeerMallContext>();
                        // 获取库存服务
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                        // 1. 定义超时时间 (例如 30 分钟前创建的订单)
                        var timeoutThreshold = DateTime.Now.AddMinutes(-30);

                        // 2. 查找符合条件的订单
                        // 条件：状态是 0 (待付款) 且 创建时间早于阈值
                        var expiredOrders = await context.Orders
                            .Include(o => o.Items) // 🔥 必须包含 Items，否则无法回滚库存
                            .Where(o => o.Status == 0 && o.CreateTime < timeoutThreshold)
                            .Take(100) // 每次处理 100 条，防止一次加载太多卡死
                            .ToListAsync();
                        
                        if (expiredOrders.Any())
                        {
                            foreach (var order in expiredOrders)
                            {
                                order.Status = -1; // 取消订单
                                order.Remark += " [系统: 超时未支付自动关闭]";

                                // 使用服务回滚库存
                                var rollbackItems = order.Items.Select(i => (i.SkuId, i.Quantity));
                                await inventoryService.ReturnStockAsync(rollbackItems);

                                _logger.LogInformation($"订单 {order.OrderNo} 已超时关闭");
                            }
                        }
                        // 4. 批量保存
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理超时订单出错");
                }

                // 每 1 分钟检查一次
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}