using BeerMall.Api.Data;
using BeerMall.Api.Services;
using BeerWallWeb.Filters;
using BeerWallWeb.Middleware;
using BeerWallWeb.Services;
using BeerWallWeb.WxPay;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- 添加数据库服务 ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// 使用 Pomelo MySql 驱动
builder.Services.AddDbContext<BeerMallContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// --- 添加 CORS 服务 ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()  // 允许任何来源 (开发阶段用，生产环境要限制)
                  .AllowAnyMethod()  // 允许 GET, POST 等
                  .AllowAnyHeader(); // 允许任何 Header
        });
});

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 注入内存缓存
builder.Services.AddMemoryCache();

// 注入 HttpClient
builder.Services.AddHttpClient();

// 注册你的微信服务
builder.Services.AddSingleton<WeChatService>();

// 注册 HttpContextAccessor (RiskControlService 获取 IP 需要用到它)
builder.Services.AddHttpContextAccessor();

// 注册你的风控服务 (使用 Scoped 生命周期，因为它依赖于 Scoped 的 DbContext)
builder.Services.AddScoped<RiskControlService>();

// 注册库存服务
builder.Services.AddScoped<IInventoryService, InventoryService>();

// 处理普通超时
builder.Services.AddHostedService<OrderTimeoutService>();

// 处理拼团失败
builder.Services.AddHostedService<GroupBuyExpirationService>();

//防超卖
builder.Services.AddScoped<InventoryAtomicService>();

//微信支付 
builder.Services.Configure<WxPayOptions>(builder.Configuration.GetSection("WxPay"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WxPayOptions>>().Value);

builder.Services.AddSingleton<WxPayHttpSigner>();
builder.Services.AddSingleton<WxPayPlatformCertStore>();
builder.Services.AddScoped<WxPayService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 启用 CORS (必须在 UseStaticFiles 之后，UseAuthorization 之前)
app.UseCors("AllowAll");

app.UseHttpsRedirection();

//全局异常
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 开启静态文件中间件
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
