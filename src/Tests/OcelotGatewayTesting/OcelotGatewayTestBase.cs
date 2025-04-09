using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace OcelotGatewayTesting;

public class OcelotGatewayTestBase
{
    protected TestServer _server;
    protected HttpClient _client;
    protected IConfiguration _configuration;

    [SetUp]
    public virtual void Setup()
    {
        var builder = new WebHostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile("ocelot.json", optional: false);
            })
            .ConfigureServices(services =>
            {
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        // JWT Configuration for tests
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = "authservice",
                            ValidAudience = "microservice-clients",
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("727410b36cdc4b5c8ac91011dd2083db8381aa7b07d245b787800a5e263f525a"))
                        };
                    });
            })
            .UseStartup<Program>();

        _server = new TestServer(builder);
        _client = _server.CreateClient();
        _configuration = _server.Services.GetRequiredService<IConfiguration>();
    }

    [TearDown]
    public virtual void TearDown()
    {
        _client.Dispose();
        _server.Dispose();
    }

    protected string GenerateJwtToken(string userId = "test-user", string[] roles = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("727410b36cdc4b5c8ac91011dd2083db8381aa7b07d245b787800a5e263f525a"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: "authservice",
            audience: "microservice-clients",
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
