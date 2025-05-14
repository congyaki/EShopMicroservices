using Prometheus;

namespace Ordering.API.Metrics
{
    public class OrderingMetrics
    {
        // Total orders count
        public static readonly Gauge TotalOrders = Prometheus.Metrics.CreateGauge(
            "ordering_total_orders", 
            "Total number of orders in the system");
            
        // Order status metrics
        public static readonly Gauge OrdersByStatus = Prometheus.Metrics.CreateGauge(
            "ordering_orders_by_status", 
            "Number of orders by status",
            new GaugeConfiguration { LabelNames = new[] { "status" } });
            
        // Order processing metrics
        public static readonly Counter OrdersProcessedTotal = Prometheus.Metrics.CreateCounter(
            "ordering_orders_processed_total", 
            "Total number of orders processed",
            new CounterConfiguration { LabelNames = new[] { "status" } });
            
        // Initialize metrics
        public static void InitializeMetrics(IServiceProvider serviceProvider)
        {
            // This method can be used to initialize metrics that require data from services
            // This should be called during application startup to set initial values
        }
    }
} 