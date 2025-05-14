using Catalog.API.Metrics;
using Catalog.API.Models;
using Catalog.API.Products.CreateProduct;
using Catalog.API.Products.DeleteProducts;
using Catalog.API.Products.GetProductById;
using Catalog.API.Products.GetProducts;
using Catalog.API.Products.GetProductsByCategory;
using Catalog.API.Products.UpdateProduct;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using BuildingBlocks.Metrics;

namespace Catalog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private ISender _mediator = null!;
        private readonly PrometheusMetricsService _metricsService;

        protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
        public ProductsController(ISender mediator, PrometheusMetricsService metricsService)
        {
            _mediator = mediator;
            _metricsService = metricsService;
        }

        #region Query
        [ProducesResponseType(typeof(GetProductsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var data = await _mediator.Send(request);
                stopwatch.Stop();
                
                // Record metrics for product search
                CatalogMetrics.ProductSearchDuration.WithLabels("all-products").Observe(stopwatch.Elapsed.TotalSeconds);
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Record API error
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("get-products", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }

        [ProducesResponseType(typeof(GetProductByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            using var requestTracker = _metricsService.TrackRequest(HttpContext.Request.Method);
            
            try
            {
                var data = await _mediator.Send(new GetProductByIdQuery(id));
                
                // Record product view if found
                if (data != null && data.Product != null)
                {
                    var category = data.Product.Category.FirstOrDefault() ?? "unknown";
                    CatalogMetrics.ProductViewedTotal
                        .WithLabels(category, id.ToString())
                        .Inc();
                }
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                // Record API error
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("get-product-by-id", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }

        [ProducesResponseType(typeof(GetProductByIdResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetProductByCategory(string category)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var data = await _mediator.Send(new GetProductsByCategoryQuery(category));
                stopwatch.Stop();
                
                // Record metrics for category access and search duration
                CatalogMetrics.CategoryAccessTotal.WithLabels(category).Inc();
                CatalogMetrics.ProductSearchDuration
                    .WithLabels("by-category")
                    .Observe(stopwatch.Elapsed.TotalSeconds);
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Record API error
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("get-products-by-category", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }
        #endregion

        #region CUD
        [ProducesResponseType(typeof(CreateProductResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductCommand command)
        {
            try
            {
                var data = await _mediator.Send(command);
                
                // Record product creation metrics
                foreach (var category in command.Category)
                {
                    CatalogMetrics.ProductCreatedTotal.WithLabels(category).Inc();
                }
                
                // Record database operation
                _metricsService.RecordDatabaseOperation("create", "product", true, TimeSpan.FromMilliseconds(10));
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                // Record failed operation
                _metricsService.RecordDatabaseOperation("create", "product", false, TimeSpan.FromMilliseconds(10));
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("create-product", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }

        [ProducesResponseType(typeof(UpdateProductResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut]
        public async Task<IActionResult> UpdateProduct(UpdateProductCommand command)
        {
            try
            {
                var data = await _mediator.Send(command);
                
                // Record product update metrics
                foreach (var category in command.Category)
                {
                    CatalogMetrics.ProductUpdatedTotal.WithLabels(category).Inc();
                }
                
                // Record database operation
                _metricsService.RecordDatabaseOperation("update", "product", true, TimeSpan.FromMilliseconds(10));
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                // Record failed operation
                _metricsService.RecordDatabaseOperation("update", "product", false, TimeSpan.FromMilliseconds(10));
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("update-product", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }

        [ProducesResponseType(typeof(DeleteProductResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] IEnumerable<Guid> ids)
        {
            try
            {
                var data = await _mediator.Send(new DeleteProductsCommand(ids));
                
                // Record product deletion metrics (we don't have category info here, so using "unknown")
                CatalogMetrics.ProductDeletedTotal.WithLabels("unknown").Inc(ids.Count());
                
                // Record database operation
                _metricsService.RecordDatabaseOperation("delete", "product", true, TimeSpan.FromMilliseconds(10));
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                // Record failed operation
                _metricsService.RecordDatabaseOperation("delete", "product", false, TimeSpan.FromMilliseconds(10));
                PrometheusMetricsService.BusinessOperations
                    .WithLabels("delete-products", "failure", "catalog-service")
                    .Inc();
                throw;
            }
        }
        #endregion
    }
}
