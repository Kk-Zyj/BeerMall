using BeerWallWeb.Extensions;
using BeerWallWeb.Models;
using BeerWallWeb.WxPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerMall.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WxPayController : ControllerBase
{
    private readonly WxPayService _wxPay;

    public WxPayController(WxPayService wxPay) => _wxPay = wxPay;

    [Authorize]
    [HttpPost("prepay")]
    public async Task<IActionResult> Prepay([FromBody] PrepayRequest req)
    {
        var userId = User.GetUserId();
        Ensure(userId > 0, "登录状态无效", 45000, 401);
        Ensure(req.OrderId > 0, "orderId无效", 45001);

        var payParams = await _wxPay.CreateJsapiPrepayAsync(req.OrderId, userId);
        return Ok(payParams);
    }

    [HttpPost("notify")]
    public async Task<IActionResult> Notify()
    {
        await _wxPay.HandleNotifyAsync(Request);
        // 微信要求回调响应 200 且 body 为 {"code":"SUCCESS","message":"成功"}
        return Ok(new { code = "SUCCESS", message = "成功" });
    }

    private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
    {
        if (!condition) throw new BusinessException(message, code, httpStatus);
    }
}