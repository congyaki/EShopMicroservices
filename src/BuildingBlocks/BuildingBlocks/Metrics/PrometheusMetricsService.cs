using Microsoft.Extensions.Logging;
using Prometheus;
using Prometheus.DotNetRuntime;
using System.Diagnostics;

namespace BuildingBlocks.Metrics
{
    public class PrometheusMetricsService : IDisposable
    {
        private readonly ILogger<PrometheusMetricsService> _logger;
        private readonly IDisposable? _collector;
        
        // Common Metrics
        public static readonly Counter TotalRequests = Prometheus.Metrics.CreateCounter(
            "app_requests_total", 
            "Total number of requests received",
            new CounterConfiguration { LabelNames = new[] { "method", "endpoint", "status_code" } });
        
        public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
            "app_request_duration_seconds", 
            "Duration of requests in seconds",
            new HistogramConfiguration 
            { 
                LabelNames = new[] { "method", "endpoint" },
                Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5, 10 }
            });
        
        public static readonly Gauge ActiveRequests = Prometheus.Metrics.CreateGauge(
            "app_active_requests", 
            "Number of requests currently being processed",
            new GaugeConfiguration { LabelNames = new[] { "method" } });
        
        public static readonly Counter DatabaseOperations = Prometheus.Metrics.CreateCounter(
            "app_database_operations_total", 
            "Total number of database operations",
            new CounterConfiguration { LabelNames = new[] { "operation", "entity", "status" } });
        
        public static readonly Histogram DatabaseOperationDuration = Prometheus.Metrics.CreateHistogram(
            "app_database_operation_duration_seconds", 
            "Duration of database operations in seconds",
            new HistogramConfiguration 
            { 
                LabelNames = new[] { "operation", "entity" },
                Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5, 10 }
            });
        
        public static readonly Counter MessageBrokerOperations = Prometheus.Metrics.CreateCounter(
            "app_message_broker_operations_total", 
            "Total number of message broker operations",
            new CounterConfiguration { LabelNames = new[] { "operation", "status" } });
        
        // For business metrics specific to each service
        public static readonly Counter BusinessOperations = Prometheus.Metrics.CreateCounter(
            "app_business_operations_total", 
            "Total number of business operations",
            new CounterConfiguration { LabelNames = new[] { "operation", "status", "service" } });

        public PrometheusMetricsService(ILogger<PrometheusMetricsService> logger)
        {
            _logger = logger;
            try
            {
                // Configure DotNetRuntime metrics collection
                _collector = DotNetRuntimeStatsBuilder
                    .Default()
                    .StartCollecting();
                
                _logger.LogInformation("Prometheus runtime metrics collector initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Prometheus runtime metrics collector");
            }
        }

        public void RecordRequest(string method, string endpoint, int statusCode, TimeSpan duration)
        {
            TotalRequests.WithLabels(method, endpoint, statusCode.ToString()).Inc();
            RequestDuration.WithLabels(method, endpoint).Observe(duration.TotalSeconds);
        }

        public IDisposable TrackRequest(string method)
        {
            ActiveRequests.WithLabels(method).Inc();
            return new RequestTracker(method);
        }

        public void RecordDatabaseOperation(string operation, string entity, bool success, TimeSpan duration)
        {
            DatabaseOperations.WithLabels(operation, entity, success ? "success" : "failure").Inc();
            DatabaseOperationDuration.WithLabels(operation, entity).Observe(duration.TotalSeconds);
        }

        public void RecordMessageBrokerOperation(string operation, bool success)
        {
            MessageBrokerOperations.WithLabels(operation, success ? "success" : "failure").Inc();
        }

        public void RecordBusinessOperation(string operation, bool success, string service)
        {
            BusinessOperations.WithLabels(operation, success ? "success" : "failure", service).Inc();
        }

        public void Dispose()
        {
            _collector?.Dispose();
        }

        // Helper class to track active requests
        private class RequestTracker : IDisposable
        {
            private readonly string _method;

            public RequestTracker(string method)
            {
                _method = method;
            }

            public void Dispose()
            {
                ActiveRequests.WithLabels(_method).Dec();
            }
        }
    }
} 