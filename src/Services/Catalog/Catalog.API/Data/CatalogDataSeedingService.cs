using BuildingBlocks.Services;
using Marten;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalog.API.Data
{
    public class CatalogDataSeedingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CatalogDataSeedingService> _logger;
        private readonly CatalogSeedingOptions _options;
        private readonly ILeaderElectionService _leaderElectionService;

        public CatalogDataSeedingService(
            IServiceProvider serviceProvider,
            ILogger<CatalogDataSeedingService> logger,
            IOptions<CatalogSeedingOptions> options,
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
            _logger.LogInformation("Catalog Seeding Service đang chờ khởi động ({Seconds}s)...", _options.StartupDelaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);

            try
            {
                // Kiểm tra xem instance này có phải là leader không
                if (!await _leaderElectionService.IsLeaderAsync())
                {
                    _logger.LogInformation("Instance không phải là leader. Bỏ qua quá trình seed data.");
                    return;
                }

                _logger.LogInformation("Instance này là LEADER, bắt đầu quá trình khởi tạo dữ liệu catalog...");
                
                // Lấy store từ IServiceProvider
                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
                
                // Kiểm tra xem đã có dữ liệu trong DB chưa
                using (var checkSession = store.QuerySession())
                {
                    if (await checkSession.Query<Product>().AnyAsync(stoppingToken))
                    {
                        _logger.LogInformation("Dữ liệu catalog đã tồn tại. Bỏ qua quá trình seed data.");
                        return;
                    }
                }

                // Lấy danh sách sản phẩm
                var allProducts = CatalogSeedData.GetPreconfiguredProducts().ToList();
                var totalProducts = allProducts.Count;
                _logger.LogInformation("Chuẩn bị seed {TotalCount} sản phẩm", totalProducts);

                // Chia thành các batch nhỏ để xử lý
                int processedCount = 0;
                foreach (var batch in allProducts.Chunk(_options.BatchSize))
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // Kiểm tra lại xem instance có còn là leader không sau mỗi batch
                    if (!await _leaderElectionService.IsLeaderAsync())
                    {
                        _logger.LogWarning("Instance không còn là leader. Dừng quá trình seed data tại {Processed}/{Total} sản phẩm.", 
                            processedCount, totalProducts);
                        return;
                    }

                    using var session = store.LightweightSession();
                    session.Store<Product>(batch);
                    await session.SaveChangesAsync(stoppingToken);

                    processedCount += batch.Length;
                    _logger.LogInformation("Đã xử lý {Processed}/{Total} sản phẩm ({Percent}%)", 
                        processedCount, totalProducts, Math.Round((double)processedCount / totalProducts * 100, 2));
                    
                    // Đợi một chút giữa các batch để giảm tải cho database
                    await Task.Delay(_options.DelayBetweenBatchesMs, stoppingToken);
                }

                _logger.LogInformation("Hoàn thành khởi tạo dữ liệu catalog");
            }
            catch (Exception ex) when (!(ex is TaskCanceledException || ex is OperationCanceledException))
            {
                _logger.LogError(ex, "Lỗi trong quá trình khởi tạo dữ liệu catalog");
            }
        }
    }

    public class CatalogSeedingOptions
    {
        public int BatchSize { get; set; } = 200;
        public int DelayBetweenBatchesMs { get; set; } = 100;
        public int StartupDelaySeconds { get; set; } = 5;
    }
}