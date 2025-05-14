using Catalog.API.Models;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Catalog.API.Metrics
{
    public class CatalogMetricsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CatalogMetricsHostedService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1); // Update metrics every 1 minute

        public CatalogMetricsHostedService(
            IServiceProvider serviceProvider, 
            ILogger<CatalogMetricsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Catalog Metrics Service is starting");

            // Initial delay to allow services to start up fully
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateMetrics(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating catalog metrics");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateMetrics(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating catalog metrics");

            using var scope = _serviceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            try
            {
                // Get total product count - simplified, just count all products
                var totalProducts = await documentSession.Query<Product>()
                    .CountAsync(cancellationToken);

                // Update the total products metric
                CatalogMetrics.TotalProducts.Set(totalProducts);
                
                _logger.LogInformation($"Total products in catalog: {totalProducts}");
                _logger.LogDebug("Catalog metrics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating catalog metrics");
                throw;
            }
        }
    }
} 