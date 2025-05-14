using Prometheus;

namespace Discount.gRPC.Metrics
{
    public class DiscountMetrics
    {
        // Total coupons count
        public static readonly Gauge TotalCoupons = Prometheus.Metrics.CreateGauge(
            "discount_total_coupons", 
            "Total number of discount coupons in the system");
            
        // Active coupons count
        public static readonly Gauge ActiveCoupons = Prometheus.Metrics.CreateGauge(
            "discount_active_coupons", 
            "Number of active discount coupons");
            
        // Coupon usage metrics
        public static readonly Counter CouponUsedTotal = Prometheus.Metrics.CreateCounter(
            "discount_coupon_used_total", 
            "Total number of times a coupon has been used",
            new CounterConfiguration { LabelNames = new[] { "product_name" } });
            
        // Initialize metrics
        public static void InitializeMetrics(IServiceProvider serviceProvider)
        {
            // This method can be used to initialize metrics that require data from services
            // This should be called during application startup to set initial values
        }
    }
} 