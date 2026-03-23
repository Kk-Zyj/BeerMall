using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BeerMall.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BeerWallWeb.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateToken(User user)
        {
            return CreateTokenInternal(
                subjectId: user.Id.ToString(),
                role: "User",
                extraClaims: new[]
                {
                    new Claim("openid", user.OpenId ?? string.Empty)
                });
        }

        public string CreateAdminToken(string userName)
        {
            return CreateTokenInternal(
                subjectId: "0",
                role: "Admin",
                extraClaims: new[]
                {
                    new Claim(ClaimTypes.Name, userName)
                });
        }

        private string CreateTokenInternal(string subjectId, string role, IEnumerable<Claim>? extraClaims = null)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? "BeerMall";
            var audience = _configuration["Jwt:Audience"] ?? "BeerMallClient";
            var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey 配置缺失");
            var expireHours = int.TryParse(_configuration["Jwt:ExpireHours"], out var h) ? h : 168;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, subjectId),
                new Claim(ClaimTypes.Role, role)
            };

            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(expireHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}