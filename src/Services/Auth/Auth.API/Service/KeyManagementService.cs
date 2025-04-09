using Auth.API.Interfaces.Services;
using System.Text;

namespace Auth.API.Service
{
    public class KeyManagementService : IKeyManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KeyManagementService> _logger;
        private byte[] _currentKey;

        public KeyManagementService(IConfiguration configuration, ILogger<KeyManagementService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // In production, this would load from a secure key vault
            // For now, we'll use the key from configuration but with better handling
            _currentKey = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key not configured"));

            if (_currentKey.Length < 32)
            {
                _logger.LogWarning("JWT key is less than recommended 256 bits length");
            }
        }

        public byte[] GetCurrentSigningKey()
        {
            return _currentKey;
        }

        public void RotateKeys()
        {
            // In production, this would generate a new key, store it securely,
            // and handle key rotation policies
            _logger.LogInformation("Key rotation requested - implementing secure rotation mechanism required");
        }
    }
}
