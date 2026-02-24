using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerMall.Api.Models.DTOs;
using BeerMall.Api.Services;
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

        public OrderController(BeerMallContext context, RiskControlService riskService, IInventoryService inventoryService)
        {
            _context = context;
            _riskService = riskService;
            _inventoryService = inventoryService;
        }

        [HttpPost("create")]
        public async Task<ActionResult> Create([FromBody] CreateOrderDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. 获取购物车数据
                // 不仅要 Include Product，还要 Include ProductSku 需要存在对应规格的产品
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Include(c => c.ProductSku) // 加载 SKU 数据用于查库存和价格
                    .Where(c => c.UserId == dto.UserId)
                    .ToListAsync();

                if (!cartItems.Any()) return BadRequest("购物车为空");

                // 2. 构建库存扣减请求
                var deductRequest = cartItems.Select(c => (c.ProductSkuId, c.Quantity));
                bool deductSuccess = await _inventoryService.TryDeductStockAsync(deductRequest);

                if (!deductSuccess)
                {
                    // 如果失败，直接回滚事务并返回提示
                    // (TryDeductStockAsync 内部虽然 SaveChanges 了，但因为在 Transaction 范围内，
                    // 只要这里 Rollback，数据库里库存就不会变)
                    await transaction.RollbackAsync();
                    return BadRequest("部分商品库存不足，无法下单");
                }

                // 3. 拼团逻辑核心 
                GroupBuyInstance groupInstance = null;
                decimal discountRate = 1.0m; // 默认不打折

                // 如果用户选择“拼团购买” (OrderType != 0)
                if (dto.OrderType != 0)
                {
                    // 🔥 1. 获取全场通用规则 (ProductId == 0)
                    // 这样就实现了“不局限于单个商品”，任何订单都能拼
                    var rule = await _context.GroupBuyRules
                        .FirstOrDefaultAsync(r => r.ProductId == 0 && r.IsActive);

                    if (rule == null) return BadRequest("当前没有开启全场拼团活动");

                    // 获取数据库配置的折扣 (商家可在后台修改这个字段)
                    discountRate = rule.DiscountRate; // e.g., 0.85

                    // --- 场景 A: 发起拼单 (开团) ---
                    if (dto.OrderType == 1)
                    {
                        groupInstance = new GroupBuyInstance
                        {
                            RuleId = rule.Id,
                            InitiatorId = dto.UserId,
                            GroupNo = Guid.NewGuid().ToString("N")[..8],
                            TargetCount = rule.RequiredPeople, // 3人
                            CurrentCount = 0,
                            StartTime = DateTime.Now,
                            ExpireTime = DateTime.Now.AddHours(rule.DurationHours), // 24小时
                            Status = 0
                        };
                        _context.GroupBuyInstances.Add(groupInstance);
                        await _context.SaveChangesAsync();
                    }
                    // --- 场景 B: 参与拼单 (参团) ---
                    else if (dto.OrderType == 2)
                    {
                        if (!dto.GroupBuyId.HasValue) return BadRequest("参团ID缺失");

                        // 锁行查询，防止并发超员 (可选)
                        groupInstance = await _context.GroupBuyInstances.FindAsync(dto.GroupBuyId.Value);

                        if (groupInstance == null) return BadRequest("拼团不存在");
                        if (groupInstance.Status != 0) return BadRequest("拼团已结束");
                        if (DateTime.Now > groupInstance.ExpireTime) return BadRequest("拼团已过期");
                        if (groupInstance.CurrentCount >= groupInstance.TargetCount) return BadRequest("拼团已满员");

                        // 简单防刷：校验是否重复参团
                        bool hasJoined = await _context.Orders.AnyAsync(o => o.GroupBuyId == groupInstance.Id && o.UserId == dto.UserId);
                        if (hasJoined) return BadRequest("您已参与过该团");
                    }
                }
                // ==================  拼团逻辑核心 ==================

                // 4. 准备地址快照数据
                string rName = "", rMobile = "", rAddr = "";

                if (dto.DeliveryMethod == "self")
                {
                    rName = "自提客户"; // 也可以取用户昵称
                    rAddr = "北京总仓"; // 实际应查仓库表
                }
                else
                {
                    var address = await _context.UserAddresses.FindAsync(dto.AddressId);
                    if (address == null) return BadRequest("收货地址不存在");

                    rName = address.Name;
                    rMobile = address.Mobile;
                    rAddr = $"{address.Province}{address.City}{address.District} {address.Detail}";
                }

                // 5. 计算金额 & 校验库存
                decimal productAmount = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in cartItems)
                {
                    // 防止空指针（比如商品下架了，或者SKU被删了）
                    if (item.Product == null || item.ProductSku == null)
                        throw new Exception($"购物车中存在无效商品");

                    // 如果是拼团模式，所有商品单价 * 折扣
                    decimal finalItemPrice = item.ProductSku.Price * discountRate;

                    // 保留两位小数
                    finalItemPrice = Math.Round(finalItemPrice, 2);

                    productAmount += finalItemPrice * item.Quantity;

                    //  构建订单明细
                    orderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        SkuId = item.ProductSkuId, // 记录 SKU ID
                        ProductName = item.Product.Name,
                        ProductImage = item.Product.MainImage,
                        SpecName = item.ProductSku.SpecName, // 记录规格名 (如: 500ml)
                        Price = finalItemPrice,       // 记录下单时的单价
                        Quantity = item.Quantity
                    });
                }

                // 6. 计算运费 (保持不变，满299免运费)
                decimal freight = 0;
                if (dto.DeliveryMethod == "express") freight = 10;
                else if (dto.DeliveryMethod == "local") freight = 12;
                if (productAmount >= 299 || dto.DeliveryMethod == "self") freight = 0;

                // 7. 计算订单总价 (商品金额 + 运费 - 优惠券)
                decimal finalPrice = productAmount + freight;
                decimal couponDiscount = 0;

                // 优惠券处理逻辑
                if (dto.UserCouponId > 0)
                {
                    // 规则 1：拼团不能用券
                    if (dto.OrderType > 1)
                    {
                        return BadRequest("拼团商品不可使用优惠券");
                    }

                    // 规则 2：检查券的有效性
                    var myCoupon = await _context.UserCoupons
                        .Include(c => c.Coupon)
                        .FirstOrDefaultAsync(c => c.Id == dto.UserCouponId && c.UserId == dto.UserId);

                    if (myCoupon == null || myCoupon.Status != 0 || myCoupon.ExpireTime < DateTime.Now)
                    {
                        return BadRequest("优惠券无效或已过期");
                    }

                    if (productAmount < myCoupon.Coupon.MinPoint)
                    {
                        return BadRequest($"未满足优惠券满减门槛 (需满{myCoupon.Coupon.MinPoint})");
                    }

                    // 规则 3：计算扣减
                    couponDiscount = myCoupon.Coupon.Amount;
                    finalPrice -= couponDiscount;
                    if (finalPrice < 0) finalPrice = 0.01m; // 防止负数，最少付1分钱

                    // 规则 4：标记券为“已使用”
                    myCoupon.Status = 1;
                    myCoupon.UsedTime = DateTime.Now;
                    // 暂时不保存 OrderId，等 Order 生成了再回填，或者下面 SaveChanges 一起保存
                }

                // 7. 生成主订单
                var order = new Order
                {
                    OrderNo = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999),
                    UserId = dto.UserId,
                    ProductAmount = productAmount,
                    FreightAmount = freight,
                    CouponAmount = couponDiscount, // 需在 Order 表加这个字段
                    TotalAmount = finalPrice,
                    Status = 0, // 待付款
                    DeliveryMethod = dto.DeliveryMethod,
                    ReceiverName = rName,
                    ReceiverMobile = rMobile,
                    ReceiverAddress = rAddr,
                    Remark = dto.Remark,
                    CreateTime = DateTime.Now,
                    Items = orderItems,
                    DeviceId = dto.DeviceId,
                    OrderType = dto.OrderType,
                    GroupBuyId = groupInstance?.Id ?? dto.GroupBuyId // 无论是开团还是参团，都关联同一个 GroupID
                };

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cartItems); // 清空购物车

                // 6. 保存并提交
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 如果用了券，把 OrderId 回填给 UserCoupon (建立关联，方便退款时找回)
                if (dto.UserCouponId > 0)
                {
                    var usedCoupon = await _context.UserCoupons.FindAsync(dto.UserCouponId);
                    usedCoupon.OrderId = order.Id;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, orderId = order.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetDetail(long id, [FromQuery] long userId)
        {
            // 1. 查询订单 (包含子表 Items)
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            // 2. 校验是否存在
            if (order == null) return NotFound("订单不存在");

            // 3. 🔥 安全校验：只能看自己的订单
            if (order.UserId != userId) return Forbid();

            //  查询关联的裂变任务
            var task = await _context.FissionTasks
                .FirstOrDefaultAsync(t => t.OrderId == id);

            // 🔥 查询关联的拼团实例 (如果是拼团订单)
            GroupBuyInstance groupInstance = null;
            if (order.GroupBuyId != null)
            {
                groupInstance = await _context.GroupBuyInstances.FindAsync(order.GroupBuyId);
            }

            // 4. 返回数据
            return Ok(new
            {
                order.Id,
                order.OrderNo,
                order.Status, // 0=待付款, 1=待发货...
                order.TotalAmount,
                order.FreightAmount,
                order.ProductAmount,
                // 地址快照
                order.ReceiverName,
                order.ReceiverMobile,
                order.ReceiverAddress,
                order.DeliveryMethod,
                order.Remark,
                order.CreateTime,
                order.OrderType,
                // 商品明细
                Items = order.Items.Select(i => new
                {
                    i.Id,
                    i.ProductName,
                    i.ProductImage,
                    i.SpecName,
                    i.Price,
                    i.Quantity
                }),
                task = task,// 🔥 把任务数据传给前端
                groupBuy = groupInstance // 🔥 返回拼团进度供前端显示
            });
        }

        // ... 
        [HttpPost("{id}/pay")]
        public async Task<ActionResult> PayOrder(long id)
        {
            // 1. 查询订单
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("订单不存在");

            // 2. 校验状态 (只有待付款才能支付)
            if (order.Status != 0) return BadRequest("订单状态不正确");

            //  3. 如果是拼团单，先设为 10 (待成团)；如果是普通单，设为 1 (待发货)
            int nextStatus = (order.OrderType != 0) ? 10 : 1;

            // ==================  4. 拼团支付后回调逻辑 ==================
            if (order.OrderType != 0 && order.GroupBuyId != null)
            {
                var instance = await _context.GroupBuyInstances.FindAsync(order.GroupBuyId);
                if (instance != null && instance.Status == 0) // 确保团还在进行中
                {
                    // 1. 增加人数
                    instance.CurrentCount++;

                    // 2. 检查是否满员
                    if (instance.CurrentCount >= instance.TargetCount)
                    {
                        instance.Status = 1; // 拼团成功

                        //将当前订单状态直接设为 1 (待发货)
                        nextStatus = 1;

                        // 找到该团里 之前所有 "待成团(10)" 的订单，批量改成 "待发货(1)"
                        // 注意：排除当前订单(id)，因为当前订单还没保存，我们在下面直接赋值 nextStatus 即可
                        var pendingOrders = await _context.Orders
                            .Where(o => o.GroupBuyId == instance.Id
                                     && o.Status == 10 // 只改那些在等候中的
                                     && o.Id != id)    // 排除自己
                            .ToListAsync();

                        foreach (var pendingOrder in pendingOrders)
                        {
                            pendingOrder.Status = 1; // 释放订单，变为待发货
                        }
                    }
                }
            }

            // ==================  5. 裂变支付后回调逻辑 ================== 
            if (order.OrderType == 0)
            {
                // 1. 场景A：我是发起人 (消费 > 0，开启裂变任务)
                if (order.TotalAmount > 0)
                {
                    // 检查是否已经有任务了，防止重复创建
                    bool hasTask = await _context.FissionTasks.AnyAsync(t => t.OrderId == order.Id);
                    if (!hasTask)
                    {
                        var task = new FissionTask
                        {
                            InitiatorId = order.UserId,
                            OrderId = order.Id,
                            SourceOrderAmount = order.TotalAmount,
                            TargetThreshold = order.TotalAmount - 5, // 动态门槛：当前金额 - 5
                            TargetCount = 3,
                            Status = 0,
                            ExpireTime = DateTime.Now.AddDays(3), //  3天限时
                            ParticipantLog = "[]"
                        };
                        _context.FissionTasks.Add(task);
                    }
                }
                // 2. 场景B：我是助力者 (检查我是否有上级)
                var buyer = await _context.Users.FindAsync(order.UserId);
                if (buyer != null && buyer.InviterId != null)
                {
                    await ProcessHelpLogic(buyer.InviterId.Value, order);
                }
            }

            // 6. 更新当前订单状态
            order.Status = nextStatus;

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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound("订单不存在");

                // 🔥 1. 状态校验：允许 0(待付款), 10(待成团), 1(待发货)
                // 2(已发货), 3(完成), -1(已取消) 不允许
                if (order.Status >= 2 || order.Status == -1)
                    return BadRequest("当前状态不支持取消，请联系客服处理");

                // 记录旧状态，用于判断是否需要退款
                int oldStatus = order.Status;

                // 2. 修改订单状态
                order.Status = -1; // -1 代表已取消
                order.Remark += $" [用户{(oldStatus > 0 ? "退款" : "")}取消]";

                // ================== 🔥 核心逻辑 1：资金退款 ==================
                if (oldStatus > 0)
                {
                    // 如果订单已付款 (Status 1 或 10)，执行退款
                    // TODO: 这里调用微信/支付宝退款接口: await _paymentService.RefundAsync(order.OrderNo, order.TotalAmount);
                    // 模拟退款日志
                    Console.WriteLine($"[退款成功] 订单: {order.OrderNo}, 金额: {order.TotalAmount}");
                }

                // ================== 🔥 核心逻辑 2：处理拼团业务 ==================
                if (order.OrderType != 0 && order.GroupBuyId != null)
                {
                    var group = await _context.GroupBuyInstances.FindAsync(order.GroupBuyId);

                    // 只有当团还在进行中(0) 或 刚成功(1) 时需要处理
                    if (group != null && group.Status != -1)
                    {
                        // 场景 A：处于“待成团”状态 (Status 10)
                        // 此时取消，意味着要把坑位让出来，或者解散团
                        if (oldStatus == 10)
                        {
                            if (group.InitiatorId == order.UserId)
                            {
                                // 团长取消 -> 团直接解散
                                group.Status = -1;
                                // 注意：如果团里还有其他已付款的团员，后台定时任务 GroupBuyExpirationService 会扫描并给他们退款
                            }
                            else
                            {
                                // 团员取消 -> 仅仅退出，人数 -1
                                group.CurrentCount--;
                                if (group.CurrentCount < 0) group.CurrentCount = 0;

                                // 需要把我在 ParticipantLog (如果有) 里的记录清除吗？
                                // 如果你记录了参与者ID列表，这里需要反序列化并移除
                            }
                        }
                        // 场景 B：处于“待发货”状态 (Status 1)
                        // 此时意味着拼团已经成功了。
                        // 策略：只给当前用户退款，不减少 group.CurrentCount，也不解散团。
                        // 原因：防止一个人退款导致全团失败，商家照常发货给其他人即可。
                    }
                }

                // ==================  核心逻辑 3：处理裂变任务 ==================
                if (order.ParentTaskId != null)
                {
                    var parentTask = await _context.FissionTasks.FindAsync(order.ParentTaskId);

                    // 只有任务还没“结算完成(Status=2)”才能回滚
                    // 如果任务已经是 -1(失败) 或 0(进行中) 或 1(待领奖)，都可以回滚
                    if (parentTask != null && parentTask.Status != 2)
                    {
                        // 1. 扣减人数
                        if (parentTask.CurrentCount > 0)
                        {
                            parentTask.CurrentCount--;
                        }

                        // 2. 从 JSON 名单中移除我
                        var participants = JsonSerializer.Deserialize<List<long>>(parentTask.ParticipantLog) ?? new List<long>();
                        if (participants.Contains(order.UserId))
                        {
                            participants.Remove(order.UserId);
                            parentTask.ParticipantLog = JsonSerializer.Serialize(participants);
                        }

                        // 3. 状态降级
                        // 如果任务之前已经“成功(1)”了，但因为我退款，人数不够了，必须把状态改回“进行中(0)”
                        if (parentTask.Status == 1 && parentTask.CurrentCount < parentTask.TargetCount)
                        {
                            parentTask.Status = 0;

                            // TODO: 如果你发了“任务成功通知”，这里最好再发个“任务回退通知”
                        }
                    }
                }
                // ================== 逻辑结束 ==================

                // 3. 库存回滚 (无论什么状态取消，都要还库存)
                var deductRequest = order.Items.Select(c => (c.SkuId, c.Quantity));
                await _inventoryService.ReturnStockAsync(deductRequest);

                // 4. 优惠券回滚 查找这笔订单是否用了券
                var usedCoupon = await _context.UserCoupons.FirstOrDefaultAsync(u => u.OrderId == order.Id);

                if (usedCoupon != null)
                {
                    // 检查券本身是否过期？
                    // 策略A：即使过期了也退回，但不延期（用户只能看着过期）
                    // 策略B：如果在有效期内，退回状态0；如果已过期，退回但状态2。

                    if (usedCoupon.ExpireTime > DateTime.Now)
                    {
                        usedCoupon.Status = 0; // 变回未使用
                        usedCoupon.UsedTime = null;
                        usedCoupon.OrderId = null; // 解除绑定
                    }
                    else
                    {
                        usedCoupon.Status = 2; // 已过期
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { success = true, refunded = oldStatus > 0 });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest("取消失败：" + ex.Message);
            }
        }
        #region 拼团

        #endregion
    }
}