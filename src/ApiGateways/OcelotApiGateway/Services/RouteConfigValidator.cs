namespace OcelotApiGateway.Services
{
    public class RouteConfigValidator
    {
        private readonly ILogger<RouteConfigValidator> _logger;

        public RouteConfigValidator(ILogger<RouteConfigValidator> logger)
        {
            _logger = logger;
        }

        public void ValidateRoutes(IConfiguration configuration)
        {
            var routes = configuration.GetSection("Routes").Get<List<Dictionary<string, object>>>();

            if (routes == null || !routes.Any())
            {
                _logger.LogWarning("No routes defined in Ocelot configuration");
                return;
            }

            _logger.LogInformation("Validating {RouteCount} Ocelot routes", routes.Count);

            foreach (var route in routes)
            {
                // Safely extract the UpstreamPathTemplate
                string upstreamPathTemplate = string.Empty;
                if (route.TryGetValue("UpstreamPathTemplate", out var upstreamPathObj))
                {
                    upstreamPathTemplate = upstreamPathObj?.ToString() ?? string.Empty;
                }

                // Check if authentication options are properly configured
                if (route.TryGetValue("AuthenticationOptions", out var authOptionsObj) && authOptionsObj != null)
                {
                    var authOptions = authOptionsObj as Dictionary<string, object>;
                    string authProviderKey = string.Empty;

                    if (authOptions != null && authOptions.TryGetValue("AuthenticationProviderKey", out var authProviderObj))
                    {
                        authProviderKey = authProviderObj?.ToString() ?? string.Empty;
                    }

                    if (string.IsNullOrEmpty(authProviderKey) &&
                        upstreamPathTemplate != "/api/auth/{everything}")
                    {
                        _logger.LogWarning(
                            "Route {UpstreamPathTemplate} has no AuthenticationProviderKey defined",
                            upstreamPathTemplate);
                    }
                }

                // Validate DownstreamHostAndPorts for load balancing
                if (route.TryGetValue("DownstreamHostAndPorts", out var hostsObj) && hostsObj != null)
                {
                    var hosts = hostsObj as IEnumerable<object>;

                    if (hosts == null || !hosts.Any())
                    {
                        _logger.LogWarning("Route {UpstreamPathTemplate} has no DownstreamHostAndPorts defined",
                            upstreamPathTemplate);
                    }
                    else if (hosts.Count() > 1)
                    {
                        // Check for load balancer options when multiple hosts are defined
                        if (route.TryGetValue("LoadBalancerOptions", out var lbOptionsObj))
                        {
                            var lbOptions = lbOptionsObj as Dictionary<string, object>;

                            if (lbOptions != null && lbOptions.TryGetValue("Type", out var lbTypeObj))
                            {
                                var lbType = lbTypeObj?.ToString();
                                _logger.LogInformation(
                                    "Route {UpstreamPathTemplate} has {HostCount} hosts with {LoadBalancerType} load balancing",
                                    upstreamPathTemplate, hosts.Count(), lbType);
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Route {UpstreamPathTemplate} has multiple hosts but no LoadBalancerOptions defined",
                                upstreamPathTemplate);
                        }
                    }
                }
            }

            _logger.LogInformation("Route validation completed");
        }
    }
}
