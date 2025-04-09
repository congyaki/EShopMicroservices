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
            var routes = configuration.GetSection("Routes").Get<List<dynamic>>();

            if (routes == null || !routes.Any())
            {
                _logger.LogWarning("No routes defined in Ocelot configuration");
                return;
            }

            _logger.LogInformation("Validating {RouteCount} Ocelot routes", routes.Count);

            foreach (var route in routes)
            {
                // Ép kiểu UpstreamPathTemplate về string
                string upstreamPathTemplate = Convert.ToString(route.UpstreamPathTemplate) ?? string.Empty;

                // Check if authentication options are properly configured
                if (route.AuthenticationOptions != null)
                {
                    string authProviderKey = Convert.ToString(route.AuthenticationOptions.AuthenticationProviderKey) ?? string.Empty;

                    if (string.IsNullOrEmpty(authProviderKey) &&
                        upstreamPathTemplate != "/api/auth/{everything}")
                    {
                        _logger.LogWarning(
                            "Route {UpstreamPathTemplate} has no AuthenticationProviderKey defined",
                            upstreamPathTemplate);
                    }
                }
            }

            _logger.LogInformation("Route validation completed");
        }
    }
}
