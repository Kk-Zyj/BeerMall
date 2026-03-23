using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerMall.Api.Models.DTOs;
using BeerMall.Api.Services;
using BeerWallWeb.Extensions;
using BeerWallWeb.Models;
using BeerWallWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BeerMall.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly BeerMallContext _context;
        private readonly RiskControlService _riskService;
        private readonly IInventoryService _inventoryService;
        private readonly InventoryAtomicService _inventoryAtomic;

        // Order 错误码
        const int CART_EMPTY = 43001;
        const int STOCK_NOT_ENOUGH = 43002;
        const int GROUP_RULE_NOT_FOUND = 43003;
        const int GROUP_ID_MISSING = 43004;
        const int GROUP_INVALID = 43005;
        const int GROUP_DUP_JOIN = 43006;
        const int ADDRESS_NOT_FOUND = 43007;
        const int CART_INVALID_ITEM = 43008;
        const int GROUP_COUPON_FORBIDDEN = 43009;
        const int COUPON_INVALID = 43010;
        const int COUPON_THRESHOLD = 43011;
        const int ORDER_NOT_FOUND = 43012;
        const int ORDER_FORBIDDEN = 43013;
        const int ORDER_STATUS_INVALID = 43014;
        const int ORDER_CANCEL_NOT_ALLOWED = 43015;
        const int CODE_UNAUTHORIZED = 43016;

        public OrderController(
            BeerMallContext context,
            RiskControlService riskService,
            IInventoryService inventoryService,
            InventoryAtomicService inventoryAtomic)
        {
            _context = context;
            _riskService = riskService;
            _inventoryService = inventoryService;
            _inventoryAtomic = inventoryAtomic;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] CreateOrderDto dto)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // 1) 购物车
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.ProductSku)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            Ensure(cartItems.Any(), "购物车为空", CART_EMPTY);

            // 2) 库存扣减
            var deductRequest = cartItems.Select(c => (skuId: c.ProductSkuId, qty: c.Quantity));
            var deductSuccess = await _inventoryAtomic.TryDeductStockAtomicAsync(deductRequest);
            Ensure(deductSuccess, "部分商品库存不足，无法下单", STOCK_NOT_ENOUGH);

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
                        InitiatorId = userId,
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

                    var hasJoined = await _context.Orders.AnyAsync(o => o.GroupBuyId == groupInstance.Id && o.UserId == userId);
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
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId);

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
                Ensure(dto.OrderType == 0, "拼团商品不可使用优惠券", GROUP_COUPON_FORBIDDEN);

                var myCoupon = await _context.UserCoupons
                    .Include(c => c.Coupon)
                    .FirstOrDefaultAsync(c => c.Id == dto.UserCouponId && c.UserId == userId);

                Ensure(myCoupon != null, "优惠券无效或已过期", COUPON_INVALID);
                Ensure(myCoupon!.Status == 0 && myCoupon.ExpireTime >= DateTime.Now, "优惠券无效或已过期", COUPON_INVALID);
                Ensure(productAmount >= myCoupon.Coupon.MinPoint, $"未满足优惠券满减门槛 (需满{myCoupon.Coupon.MinPoint})", COUPON_THRESHOLD);

                couponDiscount = myCoupon.Coupon.Amount;
                finalPrice -= couponDiscount;
                if (finalPrice < 0) finalPrice = 0.01m;

                myCoupon.Status = 1;
                myCoupon.UsedTime = DateTime.Now;
            }

            // 8) 创建订单
            var order = new Order
            {
                OrderNo = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                UserId = userId,
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
                    usedCoupon.OrderId = order.Id;
                }
            }

            await _context.SaveChangesAsync();

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
        public async Task<ActionResult> GetDetail(long id)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

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
                order.GroupBuyId,
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
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
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var order = await _context.Orders.FindAsync(id);
            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);
            Ensure(order!.UserId == userId, "无权限操作该订单", ORDER_FORBIDDEN, 403);
            Ensure(order.Status == 0, "订单状态不正确", ORDER_STATUS_INVALID);

            // 注意：这里不再直接改订单状态
            // 订单状态只允许由微信支付成功回调 /api/WxPay/notify 更新
            return Ok(new
            {
                success = true,
                status = order.Status,
                message = "请使用 /api/WxPay/prepay 拉起微信支付，订单状态将在支付回调成功后自动更新"
            });
        }

        private async Task ProcessHelpLogic(long initiatorId, Order currentOrder)
        {
            var task = await _context.FissionTasks
                .FirstOrDefaultAsync(t => t.InitiatorId == initiatorId && t.Status == 0);

            if (task == null) return;

            if (DateTime.Now > task.ExpireTime)
            {
                task.Status = -1;
                return;
            }

            if (currentOrder.UserId == initiatorId) return;

            if (currentOrder.TotalAmount < task.TargetThreshold) return;

            var participants = JsonSerializer.Deserialize<List<long>>(task.ParticipantLog) ?? new List<long>();
            if (participants.Contains(currentOrder.UserId)) return;

            var riskCheck = await _riskService.CheckRiskAsync(currentOrder.UserId, currentOrder.DeviceId);
            if (!riskCheck.Pass)
            {
                currentOrder.RiskStatus = 1;
                currentOrder.RiskReason = riskCheck.Reason;
                return;
            }

            participants.Add(currentOrder.UserId);
            task.ParticipantLog = JsonSerializer.Serialize(participants);
            task.CurrentCount++;
            currentOrder.ParentTaskId = task.Id;

            if (task.CurrentCount >= task.TargetCount)
            {
                task.Status = 1;
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult> GetList([FromQuery] int type = 0)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var query = _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            switch (type)
            {
                case 1:
                    query = query.Where(o => o.Status == 0);
                    break;
                case 2:
                    query = query.Where(o => o.Status == 1);
                    break;
                case 3:
                    query = query.Where(o => o.Status == 2);
                    break;
                case 4:
                    query = query.Where(o => o.Status == 3);
                    break;
            }

            var list = await query.OrderByDescending(o => o.CreateTime)
                .Select(o => new
                {
                    o.Id,
                    o.OrderNo,
                    o.Status,
                    o.TotalAmount,
                    o.ProductAmount,
                    o.CreateTime,
                    Items = o.Items.Select(i => new
                    {
                        i.ProductId,
                        i.ProductName,
                        i.ProductImage,
                        i.SpecName,
                        i.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpPost("{id}/confirm-receipt")]
        public async Task<ActionResult> ConfirmReceipt(long id)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            var order = await _context.Orders.FindAsync(id);
            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);
            Ensure(order!.UserId == userId, "无权限操作该订单", ORDER_FORBIDDEN, 403);
            Ensure(order.Status == 2, "当前订单状态不可确认收货", ORDER_STATUS_INVALID);

            order.Status = 3;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                status = order.Status
            });
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelOrder(long id)
        {
            var userId = User.GetUserId();
            Ensure(userId > 0, "登录状态无效", CODE_UNAUTHORIZED, 401);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            Ensure(order != null, "订单不存在", ORDER_NOT_FOUND, 404);
            Ensure(order!.UserId == userId, "无权限操作该订单", ORDER_FORBIDDEN, 403);

            Ensure(!(order.Status >= 2 || order.Status == -1), "当前状态不支持取消，请联系客服处理", ORDER_CANCEL_NOT_ALLOWED);

            int oldStatus = order.Status;

            order.Status = -1;
            order.Remark += $" [用户{(oldStatus > 0 ? "退款" : "")}取消]";

            var items = order.Items.Select(i => (skuId: i.SkuId, qty: i.Quantity));
            await _inventoryAtomic.ReturnStockAtomicAsync(items);

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

        private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
        {
            if (!condition) throw new BusinessException(message, code, httpStatus);
        }
    }
}