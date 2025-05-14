using Basket.API.Data;
using Basket.API.Models;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Basket.API.Metrics
{
    public class BasketMetricsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BasketMetricsHostedService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1); // Update metrics every 1 minute

        public BasketMetricsHostedService(
            IServiceProvider serviceProvider, 
            ILogger<BasketMetricsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Basket Metrics Service is starting");

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
                    _logger.LogError(ex, "Error updating basket metrics");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateMetrics(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating basket metrics");

            using var scope = _serviceProvider.CreateScope();
            var documentSession = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
            
            try
            {
                // Get all baskets - we need to query them first, then perform operations in memory
                // to avoid the "Marten does not support LINQ operator 'GroupBy'" error
                var allBaskets = await documentSession.Query<ShoppingCart>().ToListAsync(cancellationToken);
                
                // Count total baskets
                var totalBaskets = allBaskets.Count;
                BasketMetrics.TotalBaskets.Set(totalBaskets);
                
                // Calculate total items in all baskets
                var totalItems = allBaskets.Sum(b => b.Items.Count);
                BasketMetrics.TotalItemsInBaskets.Set(totalItems);
                
                // Calculate average items per basket
                double avgItems = totalBaskets > 0 ? (double)totalItems / totalBaskets : 0;
                BasketMetrics.AverageItemsPerBasket.Set(avgItems);
                
                // Calculate total value of all baskets
                var totalValue = allBaskets.Sum(b => b.TotalPrice);
                BasketMetrics.TotalBasketValue.Set((double)totalValue);
                
                _logger.LogInformation($"Baskets metrics updated: {totalBaskets} baskets, {totalItems} items, {avgItems:F2} avg items/basket, total value: {totalValue:C}");
                _logger.LogDebug("Basket metrics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating basket metrics");
                throw;
            }
        }
    }
} 