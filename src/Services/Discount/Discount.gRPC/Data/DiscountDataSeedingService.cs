using BuildingBlocks.Services;
using Discount.gRPC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Discount.gRPC.Data
{
    public class DiscountSeedingOptions
    {
        public int StartupDelaySeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 200;
        public int DelayBetweenBatchesMs { get; set; } = 100;
    }

    public class DiscountDataSeedingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiscountDataSeedingService> _logger;
        private readonly DiscountSeedingOptions _options;
        private readonly ILeaderElectionService _leaderElectionService;

        public DiscountDataSeedingService(
            IServiceProvider serviceProvider,
            ILogger<DiscountDataSeedingService> logger,
            IOptions<DiscountSeedingOptions> options,
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
            _logger.LogInformation("Discount Seeding Service đang chờ khởi động ({Seconds}s)...", _options.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            try
            {
                // Kiểm tra xem instance này có phải là leader không
                if (!await _leaderElectionService.IsLeaderAsync())
                {
                    _logger.LogInformation("Instance không phải là leader. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Instance này là LEADER, bắt đầu quá trình khởi tạo dữ liệu discount...");

                // Lấy context từ IServiceProvider
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DiscountContext>();
                
                // Kiểm tra xem đã có dữ liệu trong DB chưa
                var hasDiscounts = await dbContext.Coupons.AnyAsync(stoppingToken);

                if (hasDiscounts)
                {
                    _logger.LogInformation("Dữ liệu discount đã tồn tại. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Bắt đầu tiến trình khởi tạo dữ liệu cho Discount Service...");

                // Seed dữ liệu mẫu
                var sampleDiscounts = new List<Coupon>
                {
                    new Coupon { ProductName = "IPhone X", Description = "Discount for IPhone X", Amount = 150 },
                    new Coupon { ProductName = "Samsung 10", Description = "Discount for Samsung 10", Amount = 100 },
                    new Coupon { ProductName = "Sony TV", Description = "Discount for Sony TV", Amount = 200 },
                    new Coupon { ProductName = "Apple Watch", Description = "Discount for Apple Watch", Amount = 50 },
                    new Coupon { ProductName = "Tesla Model X", Description = "Discount for Tesla Model X", Amount = 1000 }
                };

                await dbContext.Coupons.AddRangeAsync(sampleDiscounts, stoppingToken);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Đã khởi tạo dữ liệu mẫu cho Discount Service thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu cho Discount Service.");
            }
        }
    }
}