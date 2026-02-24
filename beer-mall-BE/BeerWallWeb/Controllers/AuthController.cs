using BeerMall.Api.Data;
using BeerMall.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System;
using System.Text;
using BeerWallWeb.Services;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly BeerMallContext _context;
    private readonly IConfiguration _configuration;
    private readonly WeChatService _weChatService; 
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(BeerMallContext context, IConfiguration configuration, WeChatService weChatService,
                              IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _weChatService = weChatService;
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        // 1. 调用微信 API，用 code 换取 openid
        // 注意：真实项目中，AppId 和 Secret 应配置在 appsettings.json
        string appId = _configuration["AppID"] ?? throw new InvalidOperationException("AppID 配置缺失");
        string secret = _configuration["AppSecret"] ?? throw new InvalidOperationException("AppSecret 配置缺失");
        string url = $"https://api.weixin.qq.com/sns/jscode2session?appid={appId}&secret={secret}&js_code={dto.Code}&grant_type=authorization_code";

        using var client = new HttpClient();
        var response = await client.GetStringAsync(url);
        var wxData = System.Text.Json.JsonSerializer.Deserialize<WxAuthResponse>(response);

        if (string.IsNullOrEmpty(wxData?.openid))
            return BadRequest("微信登录失败");

        // 2. 查库：用户是否存在？
        var user = await _context.Users.FirstOrDefaultAsync(u => u.OpenId == wxData.openid);
        if (user == null)
        {
            // 不存在则自动注册
            user = new User { OpenId = wxData.openid };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        // 3. 返回用户信息 (实际项目中这里应该返回 JWT Token)
        return Ok(new
        {
            userId = user.Id,
            nickName = user.NickName,
            avatarUrl = user.AvatarUrl
        });
    }
    
    /// <summary>
    /// 绑定手机号 (需要 access_token)
    /// </summary>
    [HttpPost("bind-mobile")]
    public async Task<ActionResult> BindMobile([FromBody] BindMobileDto dto)
    {
        try
        {
            // 3. 🔥 核心：像调本地方法一样直接获取 Token
            // 无论缓存有没有，Service 内部都会处理好，返回给你可用的 Token
            string accessToken = await _weChatService.GetWeChatAccessTokenAsync();

            // 4. 拿着 Token 去向微信换取手机号
            string url = $"https://api.weixin.qq.com/wxa/business/getuserphonenumber?access_token={accessToken}";

            var client = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { code = dto.Code }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var resString = await response.Content.ReadAsStringAsync();

            // 反序列化微信返回的结果
            var wxRes = JsonSerializer.Deserialize<WxPhoneResponse>(resString);

            // 检查微信是否报错 (errcode != 0)
            if (wxRes == null || wxRes.errcode != 0)
            {
                return BadRequest($"微信接口错误: {wxRes?.errmsg} (Code: {dto.Code})");
            }

            string mobile = wxRes.phone_info.phoneNumber;

            // 5. 找到用户并更新数据库
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound("用户不存在");

            user.Mobile = mobile;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, mobile = mobile });
        }
        catch (Exception ex)
        {
            return BadRequest("绑定失败: " + ex.Message);
        }
    }
} 

 
public class LoginDto { public string Code { get; set; } }
public class WxAuthResponse { public string openid { get; set; } public string session_key { get; set; } }

// DTO 类
public class BindMobileDto
{
    public long UserId { get; set; }
    public string Code { get; set; } // 手机号授权的 code
}

// 微信返回结构辅助类
public class WxPhoneResponse
{
    public int errcode { get; set; }
    public string errmsg { get; set; }
    public PhoneInfo phone_info { get; set; }
}
public class PhoneInfo { public string phoneNumber { get; set; } }