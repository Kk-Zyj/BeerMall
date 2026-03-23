using System.Security.Claims;

namespace BeerWallWeb.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(value, out var userId) ? userId : 0;
        }
    }
}