// BeerWallWeb/Models/ApiResponse.cs
namespace BeerWallWeb.Models;

public class ApiResponse
{
    public int Code { get; set; } = 0;          // 0=成功，其它=失败
    public string Message { get; set; } = "ok"; // 文本提示
    public object? Data { get; set; } = null;   // 返回数据
    public string? TraceId { get; set; } = null;

    public static ApiResponse Ok(object? data = null, string message = "ok", string? traceId = null)
        => new() { Code = 0, Message = message, Data = data, TraceId = traceId };

    public static ApiResponse Fail(int code, string message, string? traceId = null, object? data = null)
        => new() { Code = code, Message = message, Data = data, TraceId = traceId };
}

public class ApiResponse<T>
{
    public int Code { get; set; } = 0;
    public string Message { get; set; } = "ok";
    public T? Data { get; set; } = default;
    public string? TraceId { get; set; } = null;

    public static ApiResponse<T> Ok(T? data, string message = "ok", string? traceId = null)
        => new() { Code = 0, Message = message, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(int code, string message, string? traceId = null, T? data = default)
        => new() { Code = code, Message = message, Data = data, TraceId = traceId };
}