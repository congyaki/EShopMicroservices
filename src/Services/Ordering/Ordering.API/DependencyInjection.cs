using BuildingBlocks.Exceptions.Handler;
using BuildingBlocks.Extensions;
using BuildingBlocks.Services;
using Consul;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Ordering.API.Data;
using Prometheus;
using Ordering.API.Metrics;

namespace Ordering.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices (this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            // Add Prometheus monitoring
            services.AddPrometheusMonitoring("ordering-service");

            // Add metrics background service
            services.AddHostedService<OrderingMetricsHostedService>();

            // Add CORS policy to allow Prometheus to scrape metrics
            services.AddCors(options =>
            {
                options.AddPolicy("MetricsPolicy", corsBuilder =>
                {
                    corsBuilder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Content-Type");
                });
            });

            // Enhanced health checks with Prometheus metrics
            services.AddHealthChecks()
                .AddSqlServer(configuration.GetConnectionString("Database")!)
                .ForwardToPrometheus();

            // Lấy cấu hình chung cho tất cả môi trường - cần thiết cho cả Development và Production
            var serviceConfig = configuration.GetServiceConfig();
            var logger = services.BuildServiceProvider().GetService<ILogger<Program>>();
            logger?.LogInformation("Configuring Ordering service: {ServiceName} at {ServiceAddress}:{ServicePort}", 
                serviceConfig.ServiceName, serviceConfig.ServiceAddress, serviceConfig.ServicePort);

            // Chỉ cấu hình seeding, Consul và Leader Election trong môi trường Development
            if (environment.IsDevelopment())
            {
                // Tạo service ID duy nhất từ serviceConfig - chỉ cần trong môi trường Development
                var serviceId = $"{serviceConfig.ServiceName}-{Guid.NewGuid()}";
                logger?.LogInformation("Generated unique service ID for Leader Election: {ServiceId}", serviceId);
                
                // Cấu hình OrderingSeedingOptions
                services.Configure<OrderingSeedingOptions>(options =>
                {
                    configuration.GetSection("OrderingSeedingOptions").Bind(options);
                    // Cấu hình mặc định nếu không có trong appsettings
                    if (options.BatchSize <= 0) options.BatchSize = 200;
                    if (options.DelayBetweenBatchesMs <= 0) options.DelayBetweenBatchesMs = 100;
                    if (options.StartupDelaySeconds <= 0) options.StartupDelaySeconds = 5;
                });

                // Register Consul client - chỉ trong môi trường Development
                services.AddSingleton<IConsulClient>(p => new ConsulClient(consulConfig =>
                {
                    consulConfig.Address = new Uri($"http://{serviceConfig.ConsulHost}:{serviceConfig.ConsulPort}");
                }));

                // Đăng ký Leader Election Service sử dụng Factory pattern - chỉ trong môi trường Development
                services.AddLeaderElection(configuration, environment);

                // Đăng ký Background Service để seed data - chỉ trong môi trường Development
                services.AddHostedService<OrderingDataSeedingService>();

                // Register ServiceDiscoveryHostedService - chỉ trong môi trường Development
                services.AddHostedService<ServiceDiscoveryHostedService>();
                
                logger?.LogInformation("Consul client, Leader Election và Service Discovery đã được đăng ký trong môi trường Development");
            }
            else
            {
                // Trong môi trường Production, sử dụng NoOpLeaderElectionService để tránh phụ thuộc vào Consul
                services.AddSingleton<ILeaderElectionService>(sp =>
                {
                    var prodLogger = sp.GetRequiredService<ILogger<NoOpLeaderElectionService>>();
                    return new NoOpLeaderElectionService(prodLogger);
                });
                
                logger?.LogInformation("Production mode: Using {ServiceName} with NoOpLeaderElectionService", serviceConfig.ServiceName);
            }

            return services;
        }

        public static WebApplication UseApiServices (this WebApplication app)
        {
            // Đầu tiên, sử dụng CORS để cho phép Prometheus scrape metrics
            app.UseCors("MetricsPolicy");
            
            app.UseMiddleware<CustomExceptionHandler>();

            // Đăng ký Prometheus HTTP metrics middleware
            app.UseHttpMetrics();
            
            // Configure health checks
            app.UseHealthChecks("/health",
                new HealthCheckOptions()
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                
            // Không gọi UseEndpoints ở đây vì nó phải được gọi sau UseRouting
            // Thay vào đó, trả về app để các middleware khác được đăng ký đúng thứ tự
            
            // Initialize metrics
            OrderingMetrics.InitializeMetrics(app.Services);

            return app;
        }
    }
}
