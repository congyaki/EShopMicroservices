using BuildingBlocks.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ordering.Infrastructure.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.API.Data
{
    public class OrderingSeedingOptions
    {
        public int StartupDelaySeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 200;
        public int DelayBetweenBatchesMs { get; set; } = 100;
    }

    public class OrderingDataSeedingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderingDataSeedingService> _logger;
        private readonly OrderingSeedingOptions _options;
        private readonly ILeaderElectionService _leaderElectionService;

        public OrderingDataSeedingService(
            IServiceProvider serviceProvider,
            ILogger<OrderingDataSeedingService> logger,
            IOptions<OrderingSeedingOptions> options,
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
            _logger.LogInformation("Ordering Seeding Service đang chờ khởi động ({Seconds}s)...", _options.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            try
            {
                // Kiểm tra xem instance này có phải là leader không
                if (!await _leaderElectionService.IsLeaderAsync())
                {
                    _logger.LogInformation("Instance không phải là leader. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Instance này là LEADER, bắt đầu quá trình khởi tạo dữ liệu ordering...");

                // Lấy context từ IServiceProvider
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Kiểm tra xem đã có dữ liệu trong DB chưa
                var hasCustomers = await context.Customers.AnyAsync(stoppingToken);
                var hasOrders = await context.Orders.AnyAsync(stoppingToken);
                var hasProducts = await context.Products.AnyAsync(stoppingToken);

                if (hasCustomers && hasOrders && hasProducts)
                {
                    _logger.LogInformation("Dữ liệu ordering đã tồn tại. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Bắt đầu tiến trình khởi tạo dữ liệu cho Ordering Service...");

                // Seed dữ liệu khách hàng nếu chưa có
                if (!hasCustomers)
                {
                    _logger.LogInformation("Khởi tạo dữ liệu khách hàng...");
                    await context.Customers.AddRangeAsync(InitialData.Customers, stoppingToken);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Đã khởi tạo dữ liệu khách hàng thành công.");
                }

                // Seed dữ liệu sản phẩm nếu chưa có
                if (!hasProducts)
                {
                    _logger.LogInformation("Khởi tạo dữ liệu sản phẩm...");
                    await context.Products.AddRangeAsync(InitialData.Products, stoppingToken);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Đã khởi tạo dữ liệu sản phẩm thành công.");
                }

                // Seed dữ liệu đơn hàng nếu chưa có
                if (!hasOrders)
                {
                    _logger.LogInformation("Khởi tạo dữ liệu đơn hàng...");
                    await context.Orders.AddRangeAsync(InitialData.OrdersWithItems, stoppingToken);
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Đã khởi tạo dữ liệu đơn hàng thành công.");
                }

                _logger.LogInformation("Hoàn thành quá trình khởi tạo dữ liệu cho Ordering Service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu cho Ordering Service.");
            }
        }
    }
}