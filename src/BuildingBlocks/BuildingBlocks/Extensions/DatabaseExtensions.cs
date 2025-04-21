using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Extensions
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Configures the application to safely perform database migrations in a multi-instance environment
        /// </summary>
        /// <param name="app">The web application</param>
        /// <param name="migrationAction">The action that performs the actual migration</param>
        /// <returns>The web application</returns>
        public static WebApplication MigrateDatabaseSafely(
            this WebApplication app,
            Func<IServiceProvider, Task> migrationAction)
        {
            // Create a scope for the migration
            using var scope = app.Services.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var logger = serviceProvider.GetRequiredService<ILogger<WebApplication>>();
            
            try
            {
                // Run migration safely
                serviceProvider.MigrateDatabaseSafelyAsync(
                    async () => await migrationAction(serviceProvider),
                    logger).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database");
            }
            
            return app;
        }

        /// <summary>
        /// Performs database migration safely in a multi-instance environment using Consul for leader election
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve dependencies</param>
        /// <param name="migrationAction">The action that performs the actual migration</param>
        /// <param name="logger">Logger for diagnostic information</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task MigrateDatabaseSafelyAsync(
            this IServiceProvider serviceProvider,
            Func<Task> migrationAction,
            ILogger logger)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var consulClient = serviceProvider.GetRequiredService<IConsulClient>();
            var serviceConfig = configuration.GetServiceConfig();
            
            // Create a unique lock key for this service type
            var lockKey = $"migrations/{serviceConfig.ServiceName}";
            
            // Create a session for distributed locking
            var sessionId = await CreateConsulSessionAsync(consulClient, serviceConfig, logger);
            if (string.IsNullOrEmpty(sessionId))
            {
                logger.LogWarning("Failed to create Consul session. This instance will skip migration.");
                return;
            }
            
            try
            {
                // Try to acquire lock
                var acquireLock = await consulClient.KV.Acquire(new KVPair(lockKey)
                {
                    Session = sessionId,
                    Value = System.Text.Encoding.UTF8.GetBytes(serviceConfig.ServiceId)
                });
                
                if (acquireLock.Response)
                {
                    // We acquired the lock, perform migration
                    logger.LogInformation("Lock acquired by instance {ServiceId}. Performing database migration...", 
                        serviceConfig.ServiceId);
                    
                    try
                    {
                        await migrationAction();
                        logger.LogInformation("Database migration completed successfully by instance {ServiceId}",
                            serviceConfig.ServiceId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error performing database migration by instance {ServiceId}",
                            serviceConfig.ServiceId);
                        throw;
                    }
                }
                else
                {
                    // Another instance acquired the lock
                    logger.LogInformation("Lock already acquired by another instance. This instance will skip migration.");
                }
            }
            finally
            {
                // Always destroy the session when done
                try
                {
                    await consulClient.Session.Destroy(sessionId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error destroying Consul session {SessionId}", sessionId);
                }
            }
        }
        
        private static async Task<string> CreateConsulSessionAsync(
            IConsulClient consulClient, 
            ServiceConfig serviceConfig,
            ILogger logger)
        {
            try
            {
                // Create a session with a TTL
                var sessionResponse = await consulClient.Session.Create(new SessionEntry
                {
                    Name = $"{serviceConfig.ServiceName}-migrations-{serviceConfig.ServiceId}",
                    TTL = TimeSpan.FromMinutes(5), // Session will expire after 5 minutes
                    Behavior = SessionBehavior.Release // Auto release lock when session expires
                });
                
                return sessionResponse.Response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating Consul session");
                return null;
            }
        }
    }
}