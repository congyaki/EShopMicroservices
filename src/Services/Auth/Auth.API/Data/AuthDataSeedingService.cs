using Auth.API.Entities;
using BuildingBlocks.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.API.Data
{
    public class AuthSeedingOptions
    {
        public int StartupDelaySeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 200;
        public int DelayBetweenBatchesMs { get; set; } = 100;
    }

    public class AuthDataSeedingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthDataSeedingService> _logger;
        private readonly AuthSeedingOptions _options;
        private readonly ILeaderElectionService _leaderElectionService;

        public AuthDataSeedingService(
            IServiceProvider serviceProvider,
            ILogger<AuthDataSeedingService> logger,
            IOptions<AuthSeedingOptions> options,
            ILeaderElectionService leaderElectionService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
            _leaderElectionService = leaderElectionService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Delay để đảm bảo ứng dụng đã khởi động hoàn tất và database đã sẵn sàng
            _logger.LogInformation("Auth Seeding Service đang chờ khởi động ({Seconds}s)...", _options.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            try
            {
                // Kiểm tra xem instance này có phải là leader không
                if (!await _leaderElectionService.IsLeaderAsync())
                {
                    _logger.LogInformation("Instance không phải là leader. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Instance này là LEADER, bắt đầu quá trình khởi tạo dữ liệu auth...");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

                // Kiểm tra xem đã có dữ liệu trong database chưa
                if (await dbContext.Users.AnyAsync(stoppingToken))
                {
                    _logger.LogInformation("Dữ liệu auth đã tồn tại, bỏ qua quá trình seed.");
                    return;
                }

                // Gọi seed data
                var seeder = scope.ServiceProvider.GetRequiredService<AuthSeedData>();
                await seeder.SeedAsync(dbContext, stoppingToken);

                _logger.LogInformation("Đã khởi tạo xong dữ liệu auth.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong quá trình khởi tạo dữ liệu auth.");
            }
        }
    }
}