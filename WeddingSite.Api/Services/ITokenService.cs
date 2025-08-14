using WeddingSite.Api.Data;

namespace WeddingSite.Api.Services
{
    public interface ITokenService
    {

        TokenResponse GenerateTokens(ApplicationUser user);

        ApplicationUser? ValidateRefreshToken(string refreshToken);

        void RevokeRefreshToken(string userId);
    }
}
