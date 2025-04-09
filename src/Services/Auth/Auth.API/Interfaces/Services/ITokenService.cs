using Auth.API.DTOs;
using Auth.API.Entities;

namespace Auth.API.Interfaces.Services
{
    public interface ITokenService
    {
        Task<TokenResponse> GenerateTokensAsync(User user);
        Task<TokenResponse> RefreshTokenAsync(string refreshToken, string accessToken);
    }
}
