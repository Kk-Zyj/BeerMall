using BeerMall.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerMall.Api.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly BeerMallContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(BeerMallContext context, ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> TryDeductStockAsync(IEnumerable<(long SkuId, int Quantity)> items)
        {
            // 1. 获取所有涉及的 SKU ID
            var skuIds = items.Select(x => x.SkuId).ToList();

            // 2. 一次性查出所有 SKU 信息 (避免循环查库)
            // 使用 Tracking 模式，因为我们需要修改它
            var skus = await _context.ProductSkus
                .Where(s => skuIds.Contains(s.Id))
                .ToListAsync();

            // 3. 逐个检查并扣减
            foreach (var item in items)
            {
                var sku = skus.FirstOrDefault(s => s.Id == item.SkuId);

                // 如果商品不存在，或者库存不足
                if (sku == null || sku.Stock < item.Quantity)
                {
                    _logger.LogWarning($"库存扣减失败: SKU {item.SkuId} 库存不足或不存在。当前: {sku?.Stock}, 需求: {item.Quantity}");
                    return false; // 只要有一个不足，整个操作失败
                }

                // 执行扣减
                sku.Stock -= item.Quantity;
            }

            // 4. 保存更改
            // 注意：如果是在 Controller 的事务中调用此方法，这里的 SaveChanges 只是将变更发送到数据库，
            // 真正的 Commit 还是由 Controller 的 transaction.CommitAsync() 决定。
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task ReturnStockAsync(IEnumerable<(long SkuId, int Quantity)> items)
        {
            if (items == null || !items.Any()) return;

            var skuIds = items.Select(x => x.SkuId).ToList();
            var skus = await _context.ProductSkus
                .Where(s => skuIds.Contains(s.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                var sku = skus.FirstOrDefault(s => s.Id == item.SkuId);
                if (sku != null)
                {
                    sku.Stock += item.Quantity; // 加回库存
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"已回滚 {items.Count()} 个 SKU 的库存");
        }
    }
}