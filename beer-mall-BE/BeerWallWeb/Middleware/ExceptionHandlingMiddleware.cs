// BeerWallWeb/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using BeerWallWeb.Models;

namespace BeerWallWeb.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessException ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogWarning(ex, "BusinessException TraceId={TraceId}", traceId);

            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = ex.HttpStatus;

            var payload = ApiResponse.Fail(ex.Code, ex.Message, traceId);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception ex)
        {
            var traceId = context.TraceIdentifier;
            _logger.LogError(ex, "UnhandledException TraceId={TraceId}", traceId);

            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // 生产环境不要把异常明细返回给客户端
            var msg = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? ex.Message
                : "服务器内部错误";

            var payload = ApiResponse.Fail(50000, msg, traceId);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}