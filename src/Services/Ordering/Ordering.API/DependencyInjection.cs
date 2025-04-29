using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.Services;
using Consul;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Ordering.API.Data;

namespace Ordering.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices (this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(configuration.GetConnectionString("Database")!);

            // Cấu hình OrderingSeedingOptions
            services.Configure<OrderingSeedingOptions>(options =>
            {
                configuration.GetSection("OrderingSeedingOptions").Bind(options);
                // Cấu hình mặc định nếu không có trong appsettings
                if (options.BatchSize <= 0) options.BatchSize = 200;
                if (options.DelayBetweenBatchesMs <= 0) options.DelayBetweenBatchesMs = 100;
                if (options.StartupDelaySeconds <= 0) options.StartupDelaySeconds = 5;
            });

            // Lấy service ID duy nhất cho instance này - sử dụng trong Leader Election
            var serviceConfig = configuration.GetServiceConfig();
            var serviceId = $"{serviceConfig.ServiceName}-{Guid.NewGuid()}";

            // Register Consul client
            services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
            {
                consulConfig.Address = new Uri($"http://{serviceConfig.ConsulHost}:{serviceConfig.ConsulPort}");
            }));

            // Đăng ký Leader Election Service sử dụng Factory pattern
            services.AddLeaderElection(configuration);

            // Đăng ký Background Service để seed data
            services.AddHostedService<OrderingDataSeedingService>();

            // Register ServiceDiscoveryHostedService
            services.AddHostedService<ServiceDiscoveryHostedService>();

            return services;
        }

        public static WebApplication UseApiServices (this WebApplication app)
        {
            app.UseMiddleware<CustomExceptionHandler>();
            app.UseHealthChecks("/health",
                new HealthCheckOptions()
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            return app;
        }
    }
}
