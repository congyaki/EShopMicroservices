using Auth.API.Data;
using Auth.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auth.API.Metrics
{
    public class AuthMetricsHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuthMetricsHostedService> _logger;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMinutes(1); // Update metrics every 1 minute

        public AuthMetricsHostedService(
            IServiceProvider serviceProvider, 
            ILogger<AuthMetricsHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auth Metrics Service is starting");

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
                    _logger.LogError(ex, "Error updating auth metrics");
                }

                // Wait for the next update interval
                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task UpdateMetrics(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating auth metrics");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            try
            {
                // Count total users
                var totalUsers = await dbContext.Users.CountAsync(cancellationToken);

                // Update the total users metric
                AuthMetrics.TotalUsers.Set(totalUsers);
                
                _logger.LogInformation($"Total users in system: {totalUsers}");
                _logger.LogDebug("Auth metrics updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auth metrics");
                throw;
            }
        }
    }
} 