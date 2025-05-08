// ApiGateways/OcelotApiGateway/Program.cs
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.Cache.CacheManager;
using Ocelot.Provider.Consul; // Add this for Consul service discovery
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đọc file ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.FromMinutes(1) // Reduce the default 5 min clock skew for tighter security
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var jti = context.Principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (string.IsNullOrEmpty(jti))
                {
                    context.Fail("JTI claim is missing from token");
                }

                // Có thể thêm kiểm tra revocation ở đây nếu cần
            },
            OnMessageReceived = context =>
            {
                // For WebSockets or SSE support if needed
                return Task.CompletedTask;
            }
        };
    });

// Thêm dịch vụ Ocelot vào container DI
var ocelotBuilder = builder.Services.AddOcelot(builder.Configuration)
    .AddPolly()
    .AddCacheManager(x => x.WithDictionaryHandle());

// Chỉ đăng ký Consul trong môi trường Development
if (builder.Environment.IsDevelopment())
{
    ocelotBuilder.AddConsul();
    Console.WriteLine("Consul service discovery đã được đăng ký trong môi trường Development");
}

// Add Health Checks for Ocelot
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure middleware pipeline
//app.UseHttpsRedirection();

// Enable authentication
app.UseAuthentication();
app.UseAuthorization();

app.UseOcelot().Wait();

// Map health checks endpoint
app.MapHealthChecks("/health");

app.Run();
