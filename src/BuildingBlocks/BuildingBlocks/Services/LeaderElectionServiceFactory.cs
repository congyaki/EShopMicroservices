using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Consul;
using Microsoft.Extensions.Hosting;

namespace BuildingBlocks.Services
{
    /// <summary>
    /// Factory để tạo dịch vụ Leader Election phù hợp dựa trên môi trường
    /// </summary>
    public static class LeaderElectionServiceFactory
    {
        /// <summary>
        /// Thêm dịch vụ Leader Election vào DI container
        /// </summary>
        public static IServiceCollection AddLeaderElection(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment hostEnvironment)
        {
            // Đọc cấu hình từ appsettings.json
            var leaderElectionConfig = configuration.GetSection("LeaderElection");
            var isEnabled = leaderElectionConfig.GetValue<bool>("Enabled", true);
            var serviceName = leaderElectionConfig["ServiceName"] ?? "unknown-service";
            
            // Tạo một ID duy nhất cho mỗi instance
            var serviceId = $"{serviceName}-{Guid.NewGuid()}";

            // Nếu đang chạy trong môi trường Production (Kubernetes) hoặc Leader Election bị vô hiệu hóa
            if (!hostEnvironment.IsDevelopment() || !isEnabled)
            {
                // Sử dụng NoOpLeaderElectionService để vô hiệu hóa Leader Election trong Production
                services.AddSingleton<ILeaderElectionService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<NoOpLeaderElectionService>>();
                    return new NoOpLeaderElectionService(logger);
                });
            }
            else
            {
                // Mặc định: Sử dụng Consul Leader Election trong môi trường development
                services.AddSingleton<IConsulClient>(sp =>
                {
                    var consulConfig = configuration.GetSection("Consul");
                    var consulAddress = consulConfig["Address"] ?? "http://consul:8500";
                    
                    return new ConsulClient(cfg =>
                    {
                        cfg.Address = new Uri(consulAddress);
                    });
                });

                services.AddSingleton<ILeaderElectionService>(sp =>
                {
                    var consulClient = sp.GetRequiredService<IConsulClient>();
                    var logger = sp.GetRequiredService<ILogger<ConsulLeaderElectionService>>();
                    
                    return new ConsulLeaderElectionService(
                        consulClient, 
                        serviceName, 
                        serviceId, 
                        logger);
                });
            }

            return services;
        }
    }
}