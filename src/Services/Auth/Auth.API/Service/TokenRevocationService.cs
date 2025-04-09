using Auth.API.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.API.Service
{
    public class TokenRevocationService : ITokenRevocationService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenRevocationService> _logger;

        public TokenRevocationService(IDistributedCache cache, ILogger<TokenRevocationService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task RevokeTokenAsync(string jti)
        {
            // Store the JTI in the blacklist with an expiration
            await _cache.SetStringAsync(
                $"revoked_token:{jti}",
                DateTime.UtcNow.ToString("O"),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) // Match refresh token expiry
                });

            _logger.LogInformation("Token with JTI {JtiId} has been revoked", jti);
        }

        public async Task<bool> IsTokenRevokedAsync(string jti)
        {
            var result = await _cache.GetStringAsync($"revoked_token:{jti}");
            return result != null;
        }
    }
}
