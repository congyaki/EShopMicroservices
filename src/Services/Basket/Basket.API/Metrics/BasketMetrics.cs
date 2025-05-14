using Prometheus;

namespace Basket.API.Metrics
{
    public class BasketMetrics
    {
        // Basket-related metrics
        public static readonly Gauge TotalBaskets = Prometheus.Metrics.CreateGauge(
            "basket_total_baskets", 
            "Total number of shopping baskets in the system");
            
        public static readonly Gauge TotalItemsInBaskets = Prometheus.Metrics.CreateGauge(
            "basket_total_items", 
            "Total number of items in all shopping baskets");
            
        public static readonly Gauge AverageItemsPerBasket = Prometheus.Metrics.CreateGauge(
            "basket_average_items_per_basket", 
            "Average number of items per shopping basket");
            
        public static readonly Gauge TotalBasketValue = Prometheus.Metrics.CreateGauge(
            "basket_total_value", 
            "Total value of all shopping baskets");
            
        public static readonly Counter BasketCreatedTotal = Prometheus.Metrics.CreateCounter(
            "basket_created_total", 
            "Total number of baskets created");
            
        public static readonly Counter BasketUpdatedTotal = Prometheus.Metrics.CreateCounter(
            "basket_updated_total", 
            "Total number of baskets updated");
            
        public static readonly Counter BasketDeletedTotal = Prometheus.Metrics.CreateCounter(
            "basket_deleted_total", 
            "Total number of baskets deleted");
            
        // Initialize metrics
        public static void InitializeMetrics(IServiceProvider serviceProvider)
        {
            // This method can be used to initialize metrics that require data from services
            // This should be called during application startup to set initial values
        }
    }
} 