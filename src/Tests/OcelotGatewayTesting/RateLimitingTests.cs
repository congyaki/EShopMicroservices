using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;

namespace OcelotGatewayTesting;

[TestFixture]
public class RateLimitingTests : OcelotGatewayTestBase
{
    [Test]
    public async Task WhenTooManyRequests_ShouldReturn429TooManyRequests()
    {
        // Arrange
        var endpoint = "/api/v1/catalog/products"; // An endpoint that has rate limiting
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act - Send requests to exceed the limit
        var successfulRequests = 0;
        var rateLimitedRequests = 0;

        // We'll make more requests than the limit to trigger rate limiting
        var totalRequests = 15; // Assuming the limit is less than this

        for (int i = 0; i < totalRequests; i++)
        {
            var response = await _client.GetAsync(endpoint);
            
            if (response.StatusCode == HttpStatusCode.OK)
                successfulRequests++;
            else if (response.StatusCode == HttpStatusCode.TooManyRequests)
                rateLimitedRequests++;

            // Small delay to allow rate limit counters to update
            await Task.Delay(50);
        }

        // Assert
        rateLimitedRequests.Should().BeGreaterThan(0, "Rate limiting should block some requests");
        successfulRequests.Should().BeGreaterThan(0, "Some requests should succeed before hitting the limit");
        (successfulRequests + rateLimitedRequests).Should().Be(totalRequests, "All requests should be either OK or rate limited");
    }

    [Test]
    public async Task ClientWhitelist_ShouldNotBeRateLimited()
    {
        // Note: This test assumes you have some whitelisted clients in your configuration
        // If you don't, you'll need to update this test or your configuration to test this feature
        
        // Arrange
        var endpoint = "/api/v1/catalog/products";
        var token = GenerateJwtToken("whitelisted-client"); // Assuming this client is whitelisted
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Act - Make many requests that would normally exceed the limit
        var responses = new List<HttpResponseMessage>();
        
        for (int i = 0; i < 20; i++)
        {
            var response = await _client.GetAsync(endpoint);
            responses.Add(response);
        }
        
        // Assert - All requests should succeed without rate limiting
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}
