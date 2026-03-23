using BeerWallWeb.Models;
using BeerWallWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeerWallWeb.Controllers.AdminController
{
    [Route("api/admin/auth")]
    [ApiController]
    public class AdminAuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly JwtTokenService _jwtTokenService;

        public AdminAuthController(IConfiguration configuration, JwtTokenService jwtTokenService)
        {
            _configuration = configuration;
            _jwtTokenService = jwtTokenService;
        }

        private const int CODE_CONFIG_MISSING = 46001;
        private const int CODE_LOGIN_FAILED = 46002;

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] AdminLoginDto dto)
        {
            Ensure(dto != null, "请求参数不能为空", CODE_LOGIN_FAILED);
            Ensure(!string.IsNullOrWhiteSpace(dto.UserName), "账号不能为空", CODE_LOGIN_FAILED);
            Ensure(!string.IsNullOrWhiteSpace(dto.Password), "密码不能为空", CODE_LOGIN_FAILED);

            var adminUserName = _configuration["AdminAuth:UserName"];
            var adminPassword = _configuration["AdminAuth:Password"];

            Ensure(!string.IsNullOrWhiteSpace(adminUserName), "后台管理员账号未配置", CODE_CONFIG_MISSING, 500);
            Ensure(!string.IsNullOrWhiteSpace(adminPassword), "后台管理员密码未配置", CODE_CONFIG_MISSING, 500);

            var ok = string.Equals(dto.UserName.Trim(), adminUserName.Trim(), StringComparison.Ordinal)
                     && string.Equals(dto.Password, adminPassword, StringComparison.Ordinal);

            Ensure(ok, "账号或密码错误", CODE_LOGIN_FAILED, 401);

            var token = _jwtTokenService.CreateAdminToken(adminUserName!);

            return Ok(new
            {
                token,
                user = new
                {
                    userName = adminUserName,
                    role = "Admin"
                }
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                userName = User.Identity?.Name ?? "admin",
                role = "Admin"
            });
        }

        private static void Ensure(bool condition, string message, int code, int httpStatus = 400)
        {
            if (!condition) throw new BusinessException(message, code, httpStatus);
        }
    }

    public class AdminLoginDto
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }
}