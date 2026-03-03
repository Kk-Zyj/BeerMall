// BeerWallWeb/Models/BusinessException.cs
namespace BeerWallWeb.Models;

public class BusinessException : Exception
{
    public int Code { get; }
    public int HttpStatus { get; }

    public BusinessException(string message, int code = 40001, int httpStatus = 400) : base(message)
    {
        Code = code;
        HttpStatus = httpStatus;
    }
}