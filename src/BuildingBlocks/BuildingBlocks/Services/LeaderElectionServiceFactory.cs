using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using k8s;
using System;
using Consul;

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
            IConfiguration configuration)
        {
            // Đọc cấu hình từ appsettings.json
            var leaderElectionConfig = configuration.GetSection("LeaderElection");
            var providerType = leaderElectionConfig["Provider"] ?? "Consul";
            var serviceName = leaderElectionConfig["ServiceName"] ?? "unknown-service";
            
            // Tạo một ID duy nhất cho mỗi instance
            var serviceId = $"{serviceName}-{Guid.NewGuid()}";

            if (string.Equals(providerType, "Kubernetes", StringComparison.OrdinalIgnoreCase))
            {
                // Sử dụng Kubernetes Leader Election trong môi trường production
                services.AddSingleton<IKubernetes>(sp =>
                {
                    // Trong Kubernetes, sử dụng in-cluster config
                    var config = KubernetesClientConfiguration.InClusterConfig();
                    return new Kubernetes(config);
                });

                services.AddSingleton<ILeaderElectionService>(sp =>
                {
                    var namespace_ = leaderElectionConfig["Namespace"] ?? "default";
                    var k8sClient = sp.GetRequiredService<IKubernetes>();
                    var logger = sp.GetRequiredService<ILogger<KubernetesLeaderElectionService>>();
                    
                    return new KubernetesLeaderElectionService(
                        k8sClient, 
                        serviceName, 
                        serviceId, 
                        namespace_, 
                        logger);
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