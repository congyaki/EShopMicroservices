using Prometheus;

namespace Catalog.API.Metrics
{
    public class CatalogMetrics
    {
        // Total products count - the only metric we're using
        public static readonly Gauge TotalProducts = Prometheus.Metrics.CreateGauge(
            "catalog_total_products", 
            "Total number of products in catalog");
        // Product-related metrics
        public static readonly Counter ProductCreatedTotal = Prometheus.Metrics.CreateCounter(
            "catalog_product_created_total", 
            "Total number of products created",
            new CounterConfiguration { LabelNames = new[] { "category" } });
        
        public static readonly Counter ProductUpdatedTotal = Prometheus.Metrics.CreateCounter(
            "catalog_product_updated_total", 
            "Total number of products updated",
            new CounterConfiguration { LabelNames = new[] { "category" } });
        
        public static readonly Counter ProductDeletedTotal = Prometheus.Metrics.CreateCounter(
            "catalog_product_deleted_total", 
            "Total number of products deleted",
            new CounterConfiguration { LabelNames = new[] { "category" } });
        
        public static readonly Gauge ProductsInStock = Prometheus.Metrics.CreateGauge(
            "catalog_products_in_stock", 
            "Number of products currently in stock",
            new GaugeConfiguration { LabelNames = new[] { "category" } });
        
        public static readonly Gauge ProductsOutOfStock = Prometheus.Metrics.CreateGauge(
            "catalog_products_out_of_stock", 
            "Number of products currently out of stock",
            new GaugeConfiguration { LabelNames = new[] { "category" } });
            
        public static readonly Counter ProductViewedTotal = Prometheus.Metrics.CreateCounter(
            "catalog_product_viewed_total", 
            "Total number of products viewed",
            new CounterConfiguration { LabelNames = new[] { "category", "product_id" } });
            
        public static readonly Histogram ProductSearchDuration = Prometheus.Metrics.CreateHistogram(
            "catalog_product_search_duration_seconds", 
            "Duration of product search operations in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "query_type" },
                Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1 }
            });
            
        // Category-related metrics
        public static readonly Gauge TotalCategories = Prometheus.Metrics.CreateGauge(
            "catalog_categories_total", 
            "Total number of product categories");
            
        public static readonly Counter CategoryAccessTotal = Prometheus.Metrics.CreateCounter(
            "catalog_category_access_total", 
            "Total number of category accesses",
            new CounterConfiguration { LabelNames = new[] { "category" } });
            
        // Initialize metrics
        public static void InitializeMetrics(IServiceProvider serviceProvider)
        {
            // This method can be used to initialize metrics that require data from services
            // This should be called during application startup to set initial values
        }
    }
} 