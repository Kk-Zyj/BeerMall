using System.Text;
using System.Text.Json;
using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using BeerWallWeb.Extensions;
using BeerWallWeb.Models;
using BeerWallWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly BeerMallContext _context;
    private readonly IConfiguration _configuration;
    private readonly WeChatService _weChatService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        BeerMallContext context,
        IConfiguration configuration,
        WeChatService weChatService,
        IHttpClientFactory httpClientFactory,
        JwtTokenService jwtTokenService)
    {
        _context = context;
        _configuration = configuration;
        _weChatService = weChatService;
        _httpClientFactory = httpClientFactory;
        _jwtTokenService = jwtTokenService;
    }

    // Auth 错误码（42xxx）
    private const int CODE_INVALID_WX_CODE = 42001;
    private const int CODE_LOGIN_FAILED = 42002;
    private const int CODE_BIND_MOBILE_FAILED = 42003;
    private const int CODE_USER_NOT_FOUND = 42004;
    private const int CODE_CONFIG_MISSING = 42005;

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        Ensure(!string.IsNullOrWhiteSpace(dto?.Code), "code不能为空", CODE_INVALID_WX_CODE);

        string? appId = _configuration["AppID"];
        string? secret = _configuration["AppSecret"];
        Ensure(!string.IsNullOrWhiteSpace(appId), "AppID 配置缺失", CODE_CONFIG_MISSING, 500);
        Ensure(!string.IsNullOrWhiteSpace(secret), "AppSecret 配置缺失", CODE_CONFIG_MISSING, 500);

        string url =
            $"https://api.weixin.qq.com/sns/jscode2session?appid={appId}&secret={secret}&js_code={dto.Code}&grant_type=authorization_code";

        var client = _httpClientFactory.CreateClient();
        string response;
        try
        {
            response = await client.GetStringAsync(url);
        }
        catch
        {
            throw new BusinessException("微信登录失败：网络异常", CODE_LOGIN_FAILED);
        }

        WxAuthResponse? wxData;
        try
        {
            wxData = JsonSerializer.Deserialize<WxAuthResponse>(response);
        }
        catch
        {
            throw new BusinessException("微信登录失败：响应解析错误", CODE_LOGIN_FAILED);
        }

        Ensure(!string.IsNullOrWhiteSpace(wxData?.openid), "微信登录失败", CODE_INVALID_WX_CODE);

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.OpenId == wxData!.openid);
            if (user == null)
            {
                user = new User { OpenId = wxData!.openid };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var token = _jwtTokenService.CreateToken(user);

            return Ok(new
            {
                token,

                // 兼容旧前端字段
                userId = user.Id,
                nickName = user.NickName,
                avatarUrl = user.AvatarUrl,
                mobile = user.Mobile,
                isMobileVerified = !string.IsNullOrWhiteSpace(user.Mobile),

                // 新结构
                user = new
                {
                    id = user.Id,
                    nickName = user.NickName,
                    avatarUrl = user.AvatarUrl,
                    mobile = user.Mobile,
                    isMobileVerified = !string.IsNullOrWhiteSpace(user.Mobile)
                }
            });
        }
        catch
        {
            throw new BusinessException("登录失败", CODE_LOGIN_FAILED);
        }
    }

    /// <summary>
    /// 绑定手机号（从 JWT 中识别当前用户）
    /// </summary>
    [Authorize]
    [HttpPost("bind-mobile")]
    public async Task<ActionResult> BindMobile([FromBody] BindMobileDto dto)
    {
        var userId = User.GetUserId();
        Ensure(userId > 0, "登录状态无效", CODE_USER_NOT_FOUND, 401);
        Ensure(!string.IsNullOrWhiteSpace(dto.Code), "code不能为空", CODE_BIND_MOBILE_FAILED);

        var user = await _context.Users.FindAsync(userId);
        Ensure(user != null, "用户不存在", CODE_USER_NOT_FOUND, 404);

        string accessToken;
        try
        {
            accessToken = await _weChatService.GetWeChatAccessTokenAsync();
        }
        catch
        {
            throw new BusinessException("绑定失败：获取微信 access_token 失败", CODE_BIND_MOBILE_FAILED);
        }

        string url = $"https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={accessToken}";

        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(new { code = dto.Code }),
            Encoding.UTF8,
            "application/json"
        );

        string resString;
        try
        {
            var response = await client.PostAsync(url, content);
            resString = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            throw new BusinessException("绑定失败：微信接口网络异常", CODE_BIND_MOBILE_FAILED);
        }

        WxPhoneResponse? wxRes;
        try
        {
            wxRes = JsonSerializer.Deserialize<WxPhoneResponse>(resString);
        }
        catch
        {
            throw new BusinessException("绑定失败：微信响应解析错误", CODE_BIND_MOBILE_FAILED);
        }

        Ensure(wxRes != null, "绑定失败：微信响应为空", CODE_BIND_MOBILE_FAILED);
        Ensure(wxRes!.errcode == 0, $"微信接口错误: {wxRes.errmsg}", CODE_BIND_MOBILE_FAILED);

        var mobile = wxRes.phone_info?.phoneNumber;
        Ensure(!string.IsNullOrWhiteSpace(mobile), "绑定失败：手机号为空", CODE_BIND_MOBILE_FAILED);

        user!.Mobile = mobile!;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            mobile,
            user = new
            {
                id = user.Id,
                nickName = user.NickName,
                avatarUrl = user.AvatarUrl,
                mobile = user.Mobile,
                isMobileVerified = true
            }
        });
    }

    private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
    {
        if (!condition) throw new BusinessException(message, code, httpStatus);
    }
}

public class LoginDto
{
    public string Code { get; set; } = "";
}

public class WxAuthResponse
{
    public string openid { get; set; } = "";
    public string session_key { get; set; } = "";
}

public class BindMobileDto
{
    public string Code { get; set; } = "";
}

public class WxPhoneResponse
{
    public int errcode { get; set; }
    public string errmsg { get; set; } = "";
    public PhoneInfo? phone_info { get; set; }
}

public class PhoneInfo
{
    public string phoneNumber { get; set; } = "";
}