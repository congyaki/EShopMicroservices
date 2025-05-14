using Discount.gRPC.Data;
using Discount.gRPC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Discount.gRPC.Metrics
{
    public class DiscountMetricsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiscountMetricsHostedService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1); // Update metrics every 1 minute

        public DiscountMetricsHostedService(
            IServiceProvider serviceProvider, 
            ILogger<DiscountMetricsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Discount Metrics Service is starting");

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
                    _logger.LogError(ex, "Error updating discount metrics");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateMetrics(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating discount metrics");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DiscountContext>();

            try
            {
                // Count total coupons
                var totalCoupons = await dbContext.Coupons.CountAsync(cancellationToken);
                DiscountMetrics.TotalCoupons.Set(totalCoupons);
                
                // Count active coupons (with amount > 0)
                var activeCoupons = await dbContext.Coupons
                    .Where(c => c.Amount > 0)
                    .CountAsync(cancellationToken);
                DiscountMetrics.ActiveCoupons.Set(activeCoupons);
                
                _logger.LogInformation($"Total coupons in system: {totalCoupons}, Active coupons: {activeCoupons}");
                _logger.LogDebug("Discount metrics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount metrics");
                throw;
            }
        }
    }
} 