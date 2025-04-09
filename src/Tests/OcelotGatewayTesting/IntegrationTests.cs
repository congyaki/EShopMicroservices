using NUnit.Framework;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using System.Text.Json;
using System.Text;

namespace OcelotGatewayTesting;

[TestFixture]
public class IntegrationTests : OcelotGatewayTestBase
{
    [Test]
    public async Task CompleteFlow_RetrieveCatalogProductsAndPlaceOrder()
    {
        // Arrange
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Act - Step 1: Get products from catalog
        var getProductsResponse = await _client.GetAsync("/api/v1/catalog/products");
        
        // Assert
        getProductsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await getProductsResponse.Content.ReadFromJsonAsync<List<dynamic>>();
        products.Should().NotBeEmpty();
        
        // Act - Step 2: Create an order with the first product
        var productId = products[0].id.ToString();
        var orderData = new
        {
            OrderName = "Test Order",
            CustomerId = Guid.NewGuid().ToString(),
            ProductId = productId,
            Quantity = 1,
            Price = 100.0m
        };
        
        var createOrderContent = new StringContent(
            JsonSerializer.Serialize(orderData), 
            Encoding.UTF8, 
            "application/json");
            
        var createOrderResponse = await _client.PostAsync("/api/v1/ordering/orders", createOrderContent);
        
        // Assert
        createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }
    
    [Test]
    public async Task AllEndpoints_ShouldRequireAuthentication()
    {
        // Arrange - No auth token
        var endpoints = new[]
        {
            "/api/v1/catalog/products",
            "/api/v1/ordering/orders",
            "/api/v1/basket/1"
        };
        
        // Act & Assert
        foreach (var endpoint in endpoints)
        {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"Endpoint {endpoint} should require authentication");
        }
    }
}
