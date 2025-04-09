using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;

namespace OcelotGatewayTesting;

[TestFixture]
public class CircuitBreakerTests : OcelotGatewayTestBase
{
    [Test]
    public async Task CircuitBreaker_ShouldOpenAfterFailures()
    {
        // This test is more of an integration test that requires broken services
        // We can test this by checking if we get the expected status code after a service fails repeatedly
        
        // Arrange
        var endpoint = "/api/v1/failing-service"; // This endpoint should be configured with circuit breaker
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Act & Assert
        
        // Note: In a real test, you might need to simulate failures by controlling the downstream service
        // Here we're assuming the endpoint will fail and trigger the circuit breaker
        
        // First, trigger the circuit breaker with multiple failed requests
        for (int i = 0; i < 5; i++) // More than ExceptionsAllowedBeforeBreaking in your config
        {
            await _client.GetAsync(endpoint);
        }
        
        // Now make another request - the circuit should be open
        var response = await _client.GetAsync(endpoint);
        
        // Circuit breaker should return a Service Unavailable when open
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
    
    [Test]
    public async Task CircuitBreaker_ShouldCloseAfterTimeout()
    {
        // This test verifies that the circuit closes after the timeout period
        
        // Arrange
        var endpoint = "/api/v1/failing-service";
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Act & Assert
        
        // First, trigger the circuit breaker with multiple failed requests
        for (int i = 0; i < 5; i++)
        {
            await _client.GetAsync(endpoint);
        }
        
        // The circuit should be open now
        var responseWithOpenCircuit = await _client.GetAsync(endpoint);
        responseWithOpenCircuit.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        
        // Wait for the circuit timeout period (e.g., 5 seconds in your config)
        await Task.Delay(TimeSpan.FromSeconds(7)); // Slightly longer than the configured timeout
        
        // After the timeout, the circuit should be half-open and allow a new attempt
        // For a proper test, you'd need to ensure the downstream service is working now
        
        // Note: In a real scenario, this test is challenging because you need to control
        // when the downstream service recovers. In a unit test, you might mock this.
        var responseAfterTimeout = await _client.GetAsync(endpoint);
        
        // The status could vary depending on whether the service is back up
        // If the service is still down, the circuit will open again
        // If it's up, the request should succeed
        responseAfterTimeout.IsSuccessStatusCode.Should().BeTrue();
    }
}
