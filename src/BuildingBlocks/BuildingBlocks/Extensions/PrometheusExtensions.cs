using BuildingBlocks.Metrics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus.DotNetRuntime;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Extensions
{
    public static class PrometheusExtensions
    {
        /// <summary>
        /// Adds Prometheus monitoring to the service
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="serviceName">The name of the service (defaults to assembly name)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPrometheusMonitoring(this IServiceCollection services, string? serviceName = null)
        {
            // If serviceName is not provided, use the assembly name
            if (string.IsNullOrEmpty(serviceName))
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                serviceName = entryAssembly?.GetName().Name ?? "unknown-service";
            }

            // Register the metrics service
            services.AddSingleton<PrometheusMetricsService>();
            
            // Add HTTP client metrics
            services.AddHttpClient();
            
            // Configure health checks to expose metrics
            services.AddHealthChecks()
                .ForwardToPrometheus();
                
            return services;
        }

        /// <summary>
        /// Configures the application to expose Prometheus metrics
        /// </summary>
        /// <param name="app">The web application</param>
        /// <returns>The web application for chaining</returns>
        public static WebApplication UsePrometheusMonitoring(this WebApplication app)
        {
            // Explicitly expose metrics endpoint with CORS allowed
            app.UseEndpoints(endpoints =>
            {
                // Explicitly map metrics endpoint to ensure it's properly registered
                endpoints.MapMetrics("/metrics").AllowAnonymous();
                
                // Add a health probe that Prometheus can check
                endpoints.MapGet("/metrics-probe", async context =>
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("Metrics endpoint is healthy!");
                });
            });
            
            // Use Prometheus HTTP request duration middleware
            app.UseHttpMetrics(options =>
            {
                options.AddCustomLabel("host", context => context.Request.Host.Host);
            });
            
            return app;
        }
        
        /// <summary>
        /// Adds custom metrics for a specific service
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configurator">Action to configure custom metrics</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCustomMetrics(this IServiceCollection services, Action<CustomMetricsBuilder> configurator)
        {
            var builder = new CustomMetricsBuilder();
            configurator(builder);
            
            foreach (var metric in builder.Metrics)
            {
                services.AddSingleton(metric);
            }
            
            return services;
        }
        
        /// <summary>
        /// Builder for custom metrics
        /// </summary>
        public class CustomMetricsBuilder
        {
            internal List<object> Metrics { get; } = new List<object>();
            
            /// <summary>
            /// Adds a counter metric
            /// </summary>
            /// <param name="name">Metric name</param>
            /// <param name="help">Help text</param>
            /// <param name="labelNames">Label names</param>
            /// <returns>The created counter</returns>
            public Counter AddCounter(string name, string help, params string[] labelNames)
            {
                var counter = Prometheus.Metrics.CreateCounter(name, help, new CounterConfiguration { LabelNames = labelNames });
                Metrics.Add(counter);
                return counter;
            }
            
            /// <summary>
            /// Adds a gauge metric
            /// </summary>
            /// <param name="name">Metric name</param>
            /// <param name="help">Help text</param>
            /// <param name="labelNames">Label names</param>
            /// <returns>The created gauge</returns>
            public Gauge AddGauge(string name, string help, params string[] labelNames)
            {
                var gauge = Prometheus.Metrics.CreateGauge(name, help, new GaugeConfiguration { LabelNames = labelNames });
                Metrics.Add(gauge);
                return gauge;
            }
            
            /// <summary>
            /// Adds a histogram metric
            /// </summary>
            /// <param name="name">Metric name</param>
            /// <param name="help">Help text</param>
            /// <param name="labelNames">Label names</param>
            /// <param name="buckets">Custom buckets (optional)</param>
            /// <returns>The created histogram</returns>
            public Histogram AddHistogram(string name, string help, string[] labelNames, double[]? buckets = null)
            {
                var config = new HistogramConfiguration { LabelNames = labelNames };
                if (buckets != null)
                {
                    config.Buckets = buckets;
                }
                
                var histogram = Prometheus.Metrics.CreateHistogram(name, help, config);
                Metrics.Add(histogram);
                return histogram;
            }
        }
    }
} 