using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ordering.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration, builder.Environment);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseApiServices();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Safe database migration with leader election - Chỉ thực hiện ở môi trường Development
    app.MigrateDatabaseSafely(async serviceProvider =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        logger.LogInformation("Starting database migration for Ordering Service");
        
        // Apply database migrations
        await context.Database.MigrateAsync();
        
        logger.LogInformation("Database migration completed");
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();
