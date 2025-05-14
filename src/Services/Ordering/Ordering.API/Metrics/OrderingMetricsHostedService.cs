using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ordering.Domain.Enums;
using Ordering.Domain.Models;
using Ordering.Infrastructure.Data;

namespace Ordering.API.Metrics
{
    public class OrderingMetricsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderingMetricsHostedService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1); // Update metrics every 1 minute

        public OrderingMetricsHostedService(
            IServiceProvider serviceProvider, 
            ILogger<OrderingMetricsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ordering Metrics Service is starting");

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
                    _logger.LogError(ex, "Error updating ordering metrics");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateMetrics(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating ordering metrics");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Count total orders
                var totalOrders = await dbContext.Orders.CountAsync(cancellationToken);
                OrderingMetrics.TotalOrders.Set(totalOrders);
                
                // Count orders by status
                var ordersByStatus = await dbContext.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);
                
                // Reset all status gauges first
                var statusValues = Enum.GetNames(typeof(OrderStatus));
                foreach (var status in statusValues)
                {
                    OrderingMetrics.OrdersByStatus.WithLabels(status).Set(0);
                }
                
                // Update counts for each status
                foreach (var statusGroup in ordersByStatus)
                {
                    // Convert enum value to string
                    var statusName = statusGroup.Status.ToString();
                    OrderingMetrics.OrdersByStatus.WithLabels(statusName).Set(statusGroup.Count);
                    _logger.LogDebug($"Orders with status '{statusName}': {statusGroup.Count}");
                }
                
                _logger.LogInformation($"Total orders in system: {totalOrders}");
                _logger.LogDebug("Ordering metrics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ordering metrics");
                throw;
            }
        }
    }
} 