// BeerWallWeb/Filters/ApiResponseFilter.cs
using BeerWallWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BeerWallWeb.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // 已经是 ApiResponse 就不重复包
        if (context.Result is ObjectResult obj1 && obj1.Value is ApiResponse)
        {
            await next();
            return;
        }

        var traceId = context.HttpContext.TraceIdentifier;

        // 1) ObjectResult（Ok(...) / BadRequest(...) / NotFound(...) 等）
        if (context.Result is ObjectResult objectResult)
        {
            var status = objectResult.StatusCode ?? 200;

            // 成功
            if (status >= 200 && status < 300)
            {
                objectResult.Value = ApiResponse.Ok(objectResult.Value, "ok", traceId);
                await next();
                return;
            }

            // 失败：message 从 Value 尽量解析
            var msg = ExtractMessage(objectResult.Value) ?? $"请求失败({status})";

            // code：用 httpStatus 映射一段区间（你也可以自定义）
            var code = status switch
            {
                400 => 40000,
                401 => 40100,
                403 => 40300,
                404 => 40400,
                _ => 50000
            };

            objectResult.Value = ApiResponse.Fail(code, msg, traceId);
            await next();
            return;
        }

        // 2) EmptyResult / StatusCodeResult
        if (context.Result is StatusCodeResult statusCodeResult)
        {
            var status = statusCodeResult.StatusCode;
            if (status >= 200 && status < 300)
            {
                context.Result = new ObjectResult(ApiResponse.Ok(null, "ok", traceId)) { StatusCode = status };
                await next();
                return;
            }

            var code = status switch
            {
                400 => 40000,
                401 => 40100,
                403 => 40300,
                404 => 40400,
                _ => 50000
            };
            context.Result = new ObjectResult(ApiResponse.Fail(code, $"请求失败({status})", traceId)) { StatusCode = status };
            await next();
            return;
        }

        // 3) 其它类型（FileResult 等）不动
        await next();
    }

    private static string? ExtractMessage(object? value)
    {
        if (value == null) return null;
        if (value is string s) return s;

        // 常见：BadRequest(new { message="xxx" })
        var prop = value.GetType().GetProperty("message")
                   ?? value.GetType().GetProperty("Message")
                   ?? value.GetType().GetProperty("error")
                   ?? value.GetType().GetProperty("Error");
        if (prop != null)
        {
            var v = prop.GetValue(value);
            return v?.ToString();
        }
        return value.ToString();
    }
}