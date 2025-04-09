using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.Cache.CacheManager;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using OcelotApiGateway.Services;

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

                // In a real implementation, check if the token has been revoked
                // using a TokenRevocationService
                if (string.IsNullOrEmpty(jti))
                {
                    context.Fail("JTI claim is missing from token");
                }

                // You would check revocation here
                // if (await tokenRevocationService.IsTokenRevokedAsync(jti))
                // {
                //     context.Fail("Token has been revoked");
                // }
            },
            OnMessageReceived = context =>
            {
                // For WebSockets or SSE support if needed
                return Task.CompletedTask;
            }
        };
    });

// Thêm dịch vụ Ocelot vào container DI với Circuit Breaker và Rate Limiting
builder.Services.AddOcelot(builder.Configuration)
    .AddPolly()       // Circuit Breaker
    .AddCacheManager(x => x.WithDictionaryHandle()); // Optional caching

var routeValidator = new RouteConfigValidator(builder.Services.BuildServiceProvider().GetRequiredService<ILogger<RouteConfigValidator>>());
routeValidator.ValidateRoutes(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure middleware pipeline
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseOcelot().Wait();

app.Run();
