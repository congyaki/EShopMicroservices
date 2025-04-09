using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;

namespace OcelotGatewayTesting;

[TestFixture]
public class JwtAuthenticationTests : OcelotGatewayTestBase
{
    [Test]
    public async Task WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No token setup
        var endpoint = "/api/v1/catalog/products"; // Protected endpoint
        
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Test]
    public async Task WithValidToken_ShouldAllowAccess()
    {
        // Arrange
        var endpoint = "/api/v1/catalog/products"; // Protected endpoint
        var token = GenerateJwtToken();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Test]
    public async Task WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var endpoint = "/api/v1/catalog/products"; // Protected endpoint
        
        // Generate an expired token (would need to modify GenerateJwtToken or create a special method)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXIiLCJqdGkiOiI5MjIwNDUzMy00YjI2LTQ3MTMtOTM5ZC0xMDhjODg0NDU4MmMiLCJleHAiOjE2MTk4MTQ0MDAsImlzcyI6IllvdXJJc3N1ZXIiLCJhdWQiOiJZb3VyQXVkaWVuY2UifQ.9xjm7vaBgO2JfGwZ-VjAP1iO5NkUQbCH_IjLF7mKY1c";
        
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);
        
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Test]
    public async Task WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var endpoint = "/api/v1/catalog/products"; // Protected endpoint
        var invalidToken = "invalid-token";
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", invalidToken);
        
        // Act
        var response = await _client.GetAsync(endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
