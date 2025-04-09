using Auth.API.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Auth.API.Service
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<TwoFactorService> _logger;

        public TwoFactorService(IDistributedCache cache, ILogger<TwoFactorService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public string GenerateTwoFactorCode(string username)
        {
            // Generate a random 6-digit code
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            // Store in cache with 10-minute expiration
            _cache.SetString(
                $"2fa:{username}",
                code,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

            _logger.LogInformation("Generated 2FA code for user {Username}", username);

            // In production, this would send via email/SMS
            return code;
        }

        public bool ValidateTwoFactorCode(string username, string code)
        {
            var storedCode = _cache.GetString($"2fa:{username}");

            if (storedCode == null)
            {
                _logger.LogWarning("No 2FA code found for user {Username}", username);
                return false;
            }

            var isValid = storedCode == code;

            if (isValid)
            {
                // Remove the code once used
                _cache.Remove($"2fa:{username}");
                _logger.LogInformation("Valid 2FA code for user {Username}", username);
            }
            else
            {
                _logger.LogWarning("Invalid 2FA code for user {Username}", username);
            }

            return isValid;
        }
    }
}
