using BuildingBlocks.Extensions;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Services
{
    public class ServiceDiscoveryHostedService : IHostedService
    {
        private readonly IConsulClient _consulClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServiceDiscoveryHostedService> _logger;
        private string _registrationId;

        public ServiceDiscoveryHostedService(IConsulClient consulClient, IConfiguration configuration, ILogger<ServiceDiscoveryHostedService> logger)
        {
            _consulClient = consulClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var serviceConfig = _configuration.GetServiceConfig();
            _registrationId = $"{serviceConfig.ServiceName}-{serviceConfig.ServiceId}";

            var registration = new AgentServiceRegistration
            {
                ID = _registrationId,
                Name = serviceConfig.ServiceName,
                Address = serviceConfig.ServiceAddress,
                Port = serviceConfig.ServicePort,
                Tags = new[] { "microservice", serviceConfig.ServiceName.ToLower() },
                Check = new AgentServiceCheck
                {
                    HTTP = $"{serviceConfig.ServiceAddress}:{serviceConfig.ServicePort}/health",
                    Interval = TimeSpan.FromSeconds(10),
                    Timeout = TimeSpan.FromSeconds(5)
                }
            };

            try
            {
                // Deregister service if it exists
                await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);

                // Register service
                await _consulClient.Agent.ServiceRegister(registration, cancellationToken);

                _logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered in Consul",
                    serviceConfig.ServiceName, _registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering service with Consul");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
                _logger.LogInformation("Service {ServiceId} deregistered from Consul", _registrationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deregistering service from Consul");
            }
        }
    }
}
