using BeerMall.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BeerWallWeb.Services;

/// <summary>
/// 原子库存扣减：防止并发超卖
/// </summary>
public class InventoryAtomicService
{
    private readonly BeerMallContext _db;

    public InventoryAtomicService(BeerMallContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 原子扣减库存：逐条执行 UPDATE ... WHERE Stock >= qty
    /// 任意一条失败 -> 返回 false（建议在外层事务中调用，失败后由外层回滚）。
    /// </summary>
    public async Task<bool> TryDeductStockAtomicAsync(IEnumerable<(long skuId, int qty)> items)
    {
        if (items == null) return true;

        var list = items.Where(x => x.qty > 0).ToList();
        if (list.Count == 0) return true;

        foreach (var (skuId, qty) in list)
        {
            var affected = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ProductSkus
                SET Stock = Stock - {qty}
                WHERE Id = {skuId} AND Stock >= {qty};
            ");

            if (affected <= 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 原子回滚库存：Stock += qty
    /// </summary>
    public async Task ReturnStockAtomicAsync(IEnumerable<(long skuId, int qty)> items)
    {
        if (items == null) return;

        var list = items.Where(x => x.qty > 0).ToList();
        if (list.Count == 0) return;

        foreach (var (skuId, qty) in list)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE ProductSkus
                SET Stock = Stock + {qty}
                WHERE Id = {skuId};
            ");
        }
    }
}