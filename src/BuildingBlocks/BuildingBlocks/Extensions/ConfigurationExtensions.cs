using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Extensions
{
    public static class ConfigurationExtensions
    {
        public static ServiceConfig GetServiceConfig(this IConfiguration configuration)
        {
            return new ServiceConfig
            {
                ServiceName = configuration["ServiceConfig:ServiceName"],
                ServiceId = configuration["ServiceConfig:ServiceId"] ?? Guid.NewGuid().ToString(),
                ServiceAddress = configuration["ServiceConfig:ServiceAddress"],
                ServicePort = int.Parse(configuration["ServiceConfig:ServicePort"]),
                ConsulHost = configuration["ServiceConfig:ConsulHost"],
                ConsulPort = int.Parse(configuration["ServiceConfig:ConsulPort"])
            };
        }
    }

    public class ServiceConfig
    {
        public string ServiceName { get; set; }
        public string ServiceId { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
        public string ConsulHost { get; set; }
        public int ConsulPort { get; set; }
    }
}
