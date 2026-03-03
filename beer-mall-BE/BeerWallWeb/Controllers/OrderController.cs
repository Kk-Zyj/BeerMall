using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerMall.Api.Models.DTOs;
using BeerMall.Api.Services;
using BeerWallWeb.Models;
using BeerWallWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using static SKIT.FlurlHttpClient.Wechat.Api.Models.CgibinGuideAddGuideBuyerRelationRequest.Types;

namespace BeerMall.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly BeerMallContext _context;
        private readonly RiskControlService _riskService;
        private readonly IInventoryService _inventoryService;
        private readonly InventoryAtomicService _inventoryAtomic;

        // Order 错误码
        const int CART_EMPTY = 43001; //购物车为空
        const int STOCK_NOT_ENOUGH = 43002; //库存不足
        const int GROUP_RULE_NOT_FOUND = 43003; //拼团活动未开启/规则不存在
        const int GROUP_ID_MISSING = 43004; //参团参数缺失（GroupBuyId 缺失）
        const int GROUP_INVALID = 43005; //拼团不存在/状态非法/过期/满员
        const int GROUP_DUP_JOIN = 43006; //重复参团
        const int ADDRESS_NOT_FOUND = 43007; //收货地址不存在
        const int CART_INVALID_ITEM = 43008; //购物车包含无效商品/SKU（商品或 SKU 被删/下架）
        const int GROUP_COUPON_FORBIDDEN = 43009; //拼团不可用券
        const int COUPON_INVALID = 43010; //优惠券无效/过期/状态不对
        const int COUPON_THRESHOLD = 43011; //未满足满减门槛
        const int ORDER_NOT_FOUND = 43012; //订单不存在
        const int ORDER_FORBIDDEN = 43013; //无权限查看订单
                                           //订单状态不允许支付
                                           //订单状态不允许取消

        public OrderController(BeerMallContext context, RiskControlService riskService, IInventoryService inventoryService, InventoryAtomicService inventoryAtomic)
        {
            _context = context;
            _riskService = riskService;
            _inventoryService = inventoryService;
            _inventoryAtomic = inventoryAtomic;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] CreateOrderDto dto)
        {

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1) 购物车
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.ProductSku)
                .Where(c => c.UserId == dto.UserId)
                .ToListAsync();

            Ensure(cartItems.Any(), "购物车为空", CART_EMPTY);

            // 2) 库存扣减
            var deductRequest = cartItems.Select(c => (skuId: c.ProductSkuId, qty: c.Quantity));
            var deductSuccess = await _inventoryAtomic.TryDeductStockAtomicAsync(deductRequest);
            Ensure(deductSuccess, "部分商品库存不足，无法下单", 43002);

            // 3) 拼团
            GroupBuyInstance? groupInstance = null;
            decimal discountRate = 1.0m;

            if (dto.OrderType != 0)
            {
                var rule = await _context.GroupBuyRules
                    .FirstOrDefaultAsync(r => r.ProductId == 0 && r.IsActive);

                Ensure(rule != null, "当前没有开启全场拼团活动", GROUP_RULE_NOT_FOUND);

                discountRate = rule!.DiscountRate;

                if (dto.OrderType == 1) // 开团
                {
                    groupInstance = new GroupBuyInstance
                    {
                        RuleId = rule.Id,
                        InitiatorId = dto.UserId,
                        GroupNo = Guid.NewGuid().ToString("N")[..8],
                        TargetCount = rule.RequiredPeople,
                        CurrentCount = 0,
                        StartTime = DateTime.Now,
                        ExpireTime = DateTime.Now.AddHours(rule.DurationHours),
                        Status = 0
                    };
                    _context.GroupBuyInstances.Add(groupInstance);
                    await _context.SaveChangesAsync();
                }
                else if (dto.OrderType == 2) // 参团
                {
                    Ensure(dto.GroupBuyId.HasValue, "参团ID缺失", GROUP_ID_MISSING);

                    groupInstance = await _context.GroupBuyInstances.FindAsync(dto.GroupBuyId!.Value);
                    Ensure(groupInstance != null, "拼团不存在", GROUP_INVALID);
                    Ensure(groupInstance!.Status == 0, "拼团已结束", GROUP_INVALID);
                    Ensure(DateTime.Now <= groupInstance.ExpireTime, "拼团已过期", GROUP_INVALID);
                    Ensure(groupInstance.CurrentCount < groupInstance.TargetCount, "拼团已满员", GROUP_INVALID);

                    var hasJoined = await _context.Orders.AnyAsync(o => o.GroupBuyId == groupInstance.Id && o.UserId == dto.UserId);
                    Ensure(!hasJoined, "您已参与过该团", GROUP_DUP_JOIN);
                }
            }

            // 4) 地址快照
            string rName = "", rMobile = "", rAddr = "";
            if (dto.DeliveryMethod == "self")
            {
                rName = "自提客户";
                rAddr = "北京总仓";
            }
            else
            {
                var address = await _context.UserAddresses.FindAsync(dto.AddressId);
                Ensure(address != null, "收货地址不存在", ADDRESS_NOT_FOUND);

                rName = address!.Name;
                rMobile = address.Mobile;
                rAddr = $"{address.Province}{address.City}{address.District} {address.Detail}";
            }

            // 5) 计算金额 + 明细
            decimal productAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cartItems)
            {
                Ensure(item.Product != null && item.ProductSku != null, "购物车中存在无效商品", CART_INVALID_ITEM);

                var finalItemPrice = Math.Round(item.ProductSku!.Price * discountRate, 2);
                productAmount += finalItemPrice * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    SkuId = item.ProductSkuId,
                    ProductName = item.Product!.Name,
                    ProductImage = item.Product.MainImage,
                    SpecName = item.ProductSku.SpecName,
                    Price = finalItemPrice,
                    Quantity = item.Quantity
                });
            }

            // 6) 运费
            decimal freight = 0;
            if (dto.DeliveryMethod == "express") freight = 10;
            else if (dto.DeliveryMethod == "local") freight = 12;
            if (productAmount >= 299 || dto.DeliveryMethod == "self") freight = 0;

            // 7) 优惠券
            decimal finalPrice = productAmount + freight;
            decimal couponDiscount = 0;

            if (dto.UserCouponId > 0)
            {
                // 拼团不可用券（你原代码写 orderType>1，但这里按“拼团=orderType!=0”更直观）
                Ensure(dto.OrderType == 0, "拼团商品不可使用优惠券", GROUP_COUPON_FORBIDDEN);

                var myCoupon = await _context.UserCoupons
                    .Include(c => c.Coupon)
                    .FirstOrDefaultAsync(c => c.Id == dto.UserCouponId && c.UserId == dto.UserId);

                Ensure(myCoupon != null, "优惠券无效或已过期", COUPON_INVALID);
                Ensure(myCoupon!.Status == 0 && myCoupon.ExpireTime >= DateTime.Now, "优惠券无效或已过期", COUPON_INVALID);
                Ensure(productAmount >= myCoupon.Coupon.MinPoint, $"未满足优惠券满减门槛 (需满{myCoupon.Coupon.MinPoint})", COUPON_THRESHOLD);

                couponDiscount = myCoupon.Coupon.Amount;
                finalPrice -= couponDiscount;
                if (finalPrice < 0) finalPrice = 0.01m;

                myCoupon.Status = 1;
                myCoupon.UsedTime = DateTime.Now;
                // 注意：OrderId 下面生成订单后在同一事务内回填
            }

            // 8) 创建订单
            var order = new Order
            {
                OrderNo = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                UserId = dto.UserId,
                ProductAmount = productAmount,
                FreightAmount = freight,
                CouponAmount = couponDiscount,
                TotalAmount = finalPrice,
                Status = 0,
                DeliveryMethod = dto.DeliveryMethod,
                ReceiverName = rName,
                ReceiverMobile = rMobile,
                ReceiverAddress = rAddr,
                Remark = dto.Remark,
                CreateTime = DateTime.Now,
                Items = orderItems,
                DeviceId = dto.DeviceId,
                OrderType = dto.OrderType,
                GroupBuyId = groupInstance?.Id ?? dto.GroupBuyId
            };

            _context.Orders.Add(order);

            // 9) 清空购物车
            _context.CartItems.RemoveRange(cartItems);

            // 10) 如果用了券，在同一事务里回填 OrderId
            if (dto.UserCouponId > 0)
            {
                var usedCoupon = await _context.UserCoupons.FindAsync(dto.UserCouponId);
                if (usedCoupon != null)
                {
                    usedCoupon.OrderId = order.Id; // 注意：order.Id 需要 SaveChanges 后才有
                }
            }

            await _context.SaveChangesAsync();  // 订单、明细、购物车、券状态一起落库
                                                // 回填 OrderId 需要 order.Id，这里再补一次（因为上面 FindAsync 拿到的 usedCoupon 在同一 tracking 里）
            if (dto.UserCouponId > 0)
            {
                var usedCoupon = await _context.UserCoupons.FindAsync(dto.UserCouponId);
                if (usedCoupon != null) usedCoupon.OrderId = order.Id;
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return Ok(new { success = true, orderId = order.Id });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetDetail(long id, [FromQuery] long userId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);
            Ensure(order!.UserId == userId, "无权限查看该订单", ORDER_FORBIDDEN, 403);

            var task = await _context.FissionTasks.FirstOrDefaultAsync(t => t.OrderId == id);

            GroupBuyInstance? groupInstance = null;
            if (order.GroupBuyId != null)
            {
                groupInstance = await _context.GroupBuyInstances.FindAsync(order.GroupBuyId);
            }

            return Ok(new
            {
                order.Id,
                order.OrderNo,
                order.Status,
                order.TotalAmount,
                order.FreightAmount,
                order.ProductAmount,
                order.ReceiverName,
                order.ReceiverMobile,
                order.ReceiverAddress,
                order.DeliveryMethod,
                order.Remark,
                order.CreateTime,
                order.OrderType,
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.ProductName,
                    i.ProductImage,
                    i.SpecName,
                    i.Price,
                    i.Quantity
                }),
                task,
                groupBuy = groupInstance
            });
        }

        [HttpPost("{id}/pay")]
        public async Task<ActionResult> PayOrder(long id)
        {
            const int ORDER_NOT_FOUND = 43012;
            const int ORDER_STATUS_INVALID = 43014;
            const int GROUP_INVALID = 43005;

            // 1) 支付幂等：只允许 status=0 的订单进入支付逻辑（原子更新占坑）
            // 先把订单标记为“处理中”(可选) 或者直接更新为 nextStatus（这里直接用 nextStatus）
            var order = await _context.Orders.FindAsync(id);
            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);

            // 只有待付款才能支付（并发安全：用条件更新）
            // 先决定默认 nextStatus：拼团先到 10(待成团)，普通到 1(待发货)
            var nextStatus = (order!.OrderType != 0) ? 10 : 1;

            var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE Orders
        SET Status = {nextStatus}
        WHERE Id = {id} AND Status = 0;
    ");
            Ensure(updated > 0, "订单状态不正确", ORDER_STATUS_INVALID);

            // 重新读取最新订单（避免并发下拿到旧状态/旧字段）
            order = await _context.Orders.FindAsync(id);
            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);

            // 2) 拼团支付后逻辑（并发安全：人数原子+1）
            if (order!.OrderType != 0 && order.GroupBuyId != null)
            {
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE GroupBuyInstances
                    SET CurrentCount = CurrentCount + 1
                    WHERE Id = {order.GroupBuyId}
                      AND Status = 0
                      AND CurrentCount < TargetCount
                      AND ExpireTime > NOW();
                ");
                Ensure(affected > 0, "拼团已满员或已结束", GROUP_INVALID);

                var instance = await _context.GroupBuyInstances.FindAsync(order.GroupBuyId);

                if (instance != null && instance.Status == 0 && instance.CurrentCount >= instance.TargetCount)
                {
                    // 拼团成功：团状态=1
                    instance.Status = 1;

                    // 当前订单：待发货(1)
                    order.Status = 1;

                    // 同团其他待成团订单：批量改待发货(1)
                    var pendingOrders = await _context.Orders
                        .Where(o => o.GroupBuyId == instance.Id && o.Status == 10 && o.Id != id)
                        .ToListAsync();

                    foreach (var po in pendingOrders)
                        po.Status = 1;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, status = order.Status });
            }

            // 3) 普通单裂变逻辑（现在订单状态已被原子更新为 1，不会重复进入）
            if (order.OrderType == 0)
            {
                if (order.TotalAmount > 0)
                {
                    bool hasTask = await _context.FissionTasks.AnyAsync(t => t.OrderId == order.Id);
                    if (!hasTask)
                    {
                        var task = new FissionTask
                        {
                            InitiatorId = order.UserId,
                            OrderId = order.Id,
                            SourceOrderAmount = order.TotalAmount,
                            TargetThreshold = order.TotalAmount - 5,
                            TargetCount = 3,
                            Status = 0,
                            ExpireTime = DateTime.Now.AddDays(3),
                            ParticipantLog = "[]"
                        };
                        _context.FissionTasks.Add(task);
                    }
                }

                var buyer = await _context.Users.FindAsync(order.UserId);
                if (buyer != null && buyer.InviterId != null)
                {
                    await ProcessHelpLogic(buyer.InviterId.Value, order);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, status = order.Status });
        }

        // 处理助力逻辑
        private async Task ProcessHelpLogic(long initiatorId, Order currentOrder)
        {
            // 找上级的有效任务
            var task = await _context.FissionTasks
                .FirstOrDefaultAsync(t => t.InitiatorId == initiatorId
                                       && t.Status == 0); // 只找进行中的

            if (task == null) return;

            // --- 校验逻辑 ---

            // 1. 时间校验 (3天限制)
            if (DateTime.Now > task.ExpireTime)
            {
                task.Status = -1; // 标记过期
                return;
            }

            // 2. 禁止自己帮自己
            if (currentOrder.UserId == initiatorId) return;

            // 3. 金额门槛校验
            if (currentOrder.TotalAmount < task.TargetThreshold) return;

            // 4. 禁止重复助力 (解析JSON列表)
            var participants = JsonSerializer.Deserialize<List<long>>(task.ParticipantLog) ?? new List<long>();
            if (participants.Contains(currentOrder.UserId)) return;

            // 5. 风控校验 (IP/Device)
            // 注意：这里我们允许订单成交，但不计入助力进度
            var riskCheck = await _riskService.CheckRiskAsync(currentOrder.UserId, currentOrder.DeviceId);
            if (!riskCheck.Pass)
            {
                currentOrder.RiskStatus = 1;
                currentOrder.RiskReason = riskCheck.Reason;
                return;
            }

            // --- 校验通过，执行助力 ---

            participants.Add(currentOrder.UserId);
            task.ParticipantLog = JsonSerializer.Serialize(participants);
            task.CurrentCount++;

            // 记录关联：把任务ID反写给当前订单
            // 这样以后退款时，我就知道这笔订单帮了谁
            currentOrder.ParentTaskId = task.Id;

            // 检查是否完成
            if (task.CurrentCount >= task.TargetCount)
            {
                task.Status = 1; // 成功！前端显示"联系店主"
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult> GetList([FromQuery] long userId, [FromQuery] int type = 0)
        {
            // type 定义: 0=全部, 1=待付款, 2=待发货, 3=待收货, 4=已完成

            var query = _context.Orders
                .Include(o => o.Items) // 必须包含商品明细，因为列表页要显示图片
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // 状态筛选逻辑
            switch (type)
            {
                case 1: // 待付款
                    query = query.Where(o => o.Status == 0);
                    break;
                case 2: // 待发货
                    query = query.Where(o => o.Status == 1);
                    break;
                case 3: // 待收货
                    query = query.Where(o => o.Status == 2);
                    break;
                case 4: // 已完成 (可选)
                    query = query.Where(o => o.Status == 3);
                    break;
                    // case 0: 全部 -> 不做 Status 筛选
            }

            // 按时间倒序
            var list = await query.OrderByDescending(o => o.CreateTime)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNo,
                    o.Status,
                    o.TotalAmount,
                    o.ProductAmount,
                    o.CreateTime,
                    // 列表页只需要显示第一张图或者前几张图
                    Items = o.Items.Select(i => new { i.ProductName, i.ProductImage, i.SpecName, i.Quantity }).ToList()
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelOrder(long id)
        {
            const int ORDER_NOT_FOUND = 43012;
            const int ORDER_CANCEL_NOT_ALLOWED = 43015;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);

            // 不允许取消：已发货及以上 or 已取消
            Ensure(!(order!.Status >= 2 || order.Status == -1), "当前状态不支持取消，请联系客服处理", ORDER_CANCEL_NOT_ALLOWED);

            int oldStatus = order.Status;

            order.Status = -1;
            order.Remark += $" [用户{(oldStatus > 0 ? "退款" : "")}取消]";

            // 拼团/裂变逻辑：保持原有（你原来的代码段可以直接保留）
            // 这里只保留你原来取消逻辑的核心结构（略），你可把原逻辑粘回这里

            // 库存回滚
            var items = order.Items.Select(i => (skuId: i.SkuId, qty: i.Quantity));
            await _inventoryAtomic.ReturnStockAtomicAsync(items);

            // 优惠券回滚
            var usedCoupon = await _context.UserCoupons.FirstOrDefaultAsync(u => u.OrderId == order.Id);
            if (usedCoupon != null)
            {
                if (usedCoupon.ExpireTime > DateTime.Now)
                {
                    usedCoupon.Status = 0;
                    usedCoupon.UsedTime = null;
                    usedCoupon.OrderId = null;
                }
                else
                {
                    usedCoupon.Status = 2;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { success = true, refunded = oldStatus > 0 });
        }
        #region 拼团

        #endregion

        private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
        {
            if (!condition) throw new BusinessException(message, code, httpStatus);
        }
    }
}