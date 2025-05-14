using Prometheus;

namespace Auth.API.Metrics
{
    public class AuthMetrics
    {
        // Total users count
        public static readonly Gauge TotalUsers = Prometheus.Metrics.CreateGauge(
            "auth_total_users", 
            "Total number of users in the system");

        // User authentication metrics
        public static readonly Counter LoginAttemptsTotal = Prometheus.Metrics.CreateCounter(
            "auth_login_attempts_total", 
            "Total number of login attempts",
            new CounterConfiguration { LabelNames = new[] { "status" } });  // status can be "success" or "failure"
            
        public static readonly Counter TokensIssuedTotal = Prometheus.Metrics.CreateCounter(
            "auth_tokens_issued_total", 
            "Total number of tokens issued");
            
        // Initialize metrics
        public static void InitializeMetrics(IServiceProvider serviceProvider)
        {
            // This method can be used to initialize metrics that require data from services
            // This should be called during application startup to set initial values
        }
    }
} 