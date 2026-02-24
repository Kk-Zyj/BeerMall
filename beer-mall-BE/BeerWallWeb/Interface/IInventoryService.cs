using BeerMall.Api.Entities;

namespace BeerMall.Api.Services
{
    public interface IInventoryService
    {
        /// <summary>
        /// 尝试扣减库存
        /// </summary>
        /// <param name="items">需要扣减的 SKU 和 数量</param>
        /// <returns>是否扣减成功 (库存不足返回 false)</returns>
        Task<bool> TryDeductStockAsync(IEnumerable<(long SkuId, int Quantity)> items);

        /// <summary>
        /// 回滚库存 (加回库存)
        /// </summary>
        /// <param name="items">需要回滚的 SKU 和 数量</param>
        Task ReturnStockAsync(IEnumerable<(long SkuId, int Quantity)> items);
    }
}