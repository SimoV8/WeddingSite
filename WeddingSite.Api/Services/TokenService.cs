using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WeddingSite.Api.Data;

namespace WeddingSite.Api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration config;
        private static readonly Dictionary<string, string> _refreshTokens = new(); // replace with DB

        public TokenService(IConfiguration config)
        {
            this.config = config;
        }

        public TokenResponse GenerateTokens(ApplicationUser user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            // store refresh token (for demo in-memory; in production use DB table)
            _refreshTokens[user.Id] = refreshToken;

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 3600
            };
        }

        public string GenerateAccessToken(ApplicationUser user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // we check expiry manually
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }

        public ApplicationUser? ValidateRefreshToken(string refreshToken)
        {
            return null;// _refreshTokens.TryGetValue(userId, out var stored) && stored == refreshToken;
        }

        public void RevokeRefreshToken(string userId)
        {
            if (_refreshTokens.TryGetValue(userId, out var stored))
            {
                _refreshTokens.Remove(userId);
            }
        }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
    }
}
