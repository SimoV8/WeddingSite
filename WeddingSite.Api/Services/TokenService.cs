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
        private readonly ApplicationDbContext applicationDbContext;

        public TokenService(IConfiguration config, ApplicationDbContext applicationDbContext)
        {
            this.config = config;
            this.applicationDbContext = applicationDbContext;
        }

        public TokenResponse GenerateTokens(ApplicationUser user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = Guid.NewGuid().ToString();

            // First delete token already associated to the user
            var toDelete = applicationDbContext.UserRefreshTokens.Where(x => x.UserId == user.Id);
            applicationDbContext.UserRefreshTokens.RemoveRange(toDelete);

            // Create a new token
            var refreshTokenObj = new UserRefreshToken() 
            { 
                RefreshToken = refreshToken, 
                CreatedAt = DateTime.Now, 
                ExpiresAt = DateTime.Now.AddDays(30), 
                UserId = user.Id,
                User = user,
            }; 

            // Store refresh token in DB
            applicationDbContext.UserRefreshTokens.Add(refreshTokenObj);
            applicationDbContext.SaveChanges();


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
            // Search the given token in DB and ensure it's not expired yet
            var token =  applicationDbContext.UserRefreshTokens.FirstOrDefault(x => x.RefreshToken == refreshToken && x.ExpiresAt >= DateTime.Now);
            // If the token is found, return the associated user
            return token?.User;
        }

        public void RevokeRefreshToken(string userId)
        {
            var toDelete = applicationDbContext.UserRefreshTokens.Where(x => x.UserId == userId);
            applicationDbContext.UserRefreshTokens.RemoveRange(toDelete);

            applicationDbContext.SaveChanges();
        }
    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
    }
}
