using System.Text;
using BeerMall.Api.Data;
using BeerMall.Api.Services;
using BeerWallWeb.Filters;
using BeerWallWeb.Middleware;
using BeerWallWeb.Services;
using BeerWallWeb.WxPay;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

ValidateStartupConfig(builder.Configuration);

// --- 数据库 ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BeerMallContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// --- CORS ---
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            if (corsOrigins.Length > 0)
            {
                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        }
        else
        {
            if (corsOrigins.Length == 0)
            {
                throw new InvalidOperationException("生产环境必须配置 Cors:AllowedOrigins");
            }

            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

// --- Controllers ---
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// 微信服务
builder.Services.AddSingleton<WeChatService>();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 风控服务
builder.Services.AddScoped<RiskControlService>();

// 库存服务
builder.Services.AddScoped<IInventoryService, InventoryService>();

// JWT
builder.Services.AddSingleton<JwtTokenService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
var jwtSecret = builder.Configuration["Jwt:SecretKey"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret!)),
            ClockSkew = TimeSpan.Zero
        };
    });

// 处理普通超时
builder.Services.AddHostedService<OrderTimeoutService>();

// 处理拼团失败
builder.Services.AddHostedService<GroupBuyExpirationService>();

// 防超卖
builder.Services.AddScoped<InventoryAtomicService>();

// 微信支付
builder.Services.Configure<WxPayOptions>(builder.Configuration.GetSection("WxPay"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WxPayOptions>>().Value);

builder.Services.AddSingleton<WxPayHttpSigner>();
builder.Services.AddSingleton<WxPayPlatformCertStore>();
builder.Services.AddScoped<WxPayService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseStaticFiles();

app.UseCors("AppCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void ValidateStartupConfig(IConfiguration configuration)
{
    static void Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("__YOUR_") || value.Contains("__APP_") || value.Contains("__DB_") || value.Contains("__PRODUCTION_"))
        {
            throw new InvalidOperationException($"配置缺失或仍为占位值：{key}");
        }
    }

    Require(configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings:DefaultConnection");
    Require(configuration["AppID"], "AppID");
    Require(configuration["AppSecret"], "AppSecret");
    Require(configuration["Jwt:Issuer"], "Jwt:Issuer");
    Require(configuration["Jwt:Audience"], "Jwt:Audience");
    Require(configuration["Jwt:SecretKey"], "Jwt:SecretKey");
    Require(configuration["AdminAuth:UserName"], "AdminAuth:UserName");
    Require(configuration["AdminAuth:Password"], "AdminAuth:Password");
}