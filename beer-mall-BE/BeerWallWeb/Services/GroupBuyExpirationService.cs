using BeerMall.Api.Data;
using Microsoft.EntityFrameworkCore;


namespace BeerMall.Api.Services
{
    /// <summary>
    /// 拼团失败自动回滚库存
    /// </summary>
    public class GroupBuyExpirationService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<GroupBuyExpirationService> _logger;

        public GroupBuyExpirationService(IServiceProvider services, ILogger<GroupBuyExpirationService> logger)
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
                        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                        // 1. 查找过期且未成功的团
                        // 条件：状态是 0 (进行中) 且 过期时间 < 当前时间
                        var failedGroups = await context.GroupBuyInstances
                            .Where(g => g.Status == 0 && g.ExpireTime < DateTime.Now)
                            .Take(50) // 批处理
                            .ToListAsync();

                        foreach (var group in failedGroups)
                        {
                            // A. 标记团失败
                            group.Status = -1; 
                            _logger.LogInformation($"拼团 {group.Id} 期限已到，判定为失败");

                            // B. 找到该团所有 "已支付/待成团" (Status=10) 的订单
                            // 注意：Status=0 的待付款订单不需要退款，直接取消即可
                            var paidOrders = await context.Orders
                                .Include(o => o.Items)
                                .Where(o => o.GroupBuyId == group.Id && o.Status == 10)
                                .ToListAsync();

                            foreach (var order in paidOrders)
                            {
                                // C. 标记订单为退款/关闭
                                order.Status = -1;
                                order.Remark += " [系统: 拼团失败退款]";

                                // D. 执行退款 (调用微信支付接口)
                                try
                                {
                                    //await _weChatPayService.RefundAsync(order.OrderNo, order.TotalAmount);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"订单 {order.OrderNo} 退款失败: {ex.Message}");
                                    // 这里可能需要标记一个"退款异常"状态，需人工介入
                                }

                                // E. 回滚库存 (货没发出去，库存要加回来)
                                var rollbackItems = order.Items.Select(i => (i.SkuId, i.Quantity));
                                await inventoryService.ReturnStockAsync(rollbackItems);
                            }

                            // F. 处理该团里那些还没付款的订单 (Status=0)
                            // 直接把它们取消掉，不需要退款
                            var unpaidOrders = await context.Orders
                                .Include(o => o.Items)
                                .Where(o => o.GroupBuyId == group.Id && o.Status == 0)
                                .ToListAsync();

                            foreach (var upOrder in unpaidOrders)
                            {
                                upOrder.Status = -1;
                                var rbItems = upOrder.Items.Select(i => (i.SkuId, i.Quantity));
                                await inventoryService.ReturnStockAsync(rbItems);
                            }
                        }

                        // G. 保存所有更改
                        if (failedGroups.Any())
                        {
                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理拼团过期任务出错");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}