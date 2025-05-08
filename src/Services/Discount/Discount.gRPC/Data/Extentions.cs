using BuildingBlocks.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Discount.gRPC.Data
{
    public static class Extentions
    {
        public static WebApplication UseMigration(this WebApplication app)
        {
            // Chỉ thực hiện migration trong môi trường Development
            if (app.Environment.IsDevelopment())
            {
                // Safe database migration with leader election
                app.MigrateDatabaseSafely(async serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<DiscountContext>>();
                    var dbContext = serviceProvider.GetRequiredService<DiscountContext>();
                    
                    logger.LogInformation("Starting database migration for Discount Service");
                    
                    // Apply database migrations
                    await dbContext.Database.MigrateAsync();
                    
                    logger.LogInformation("Database migration completed");
                });
            }
            else
            {
                var logger = app.Services.GetRequiredService<ILogger<DiscountContext>>();
                logger.LogInformation("Skipping database migration in non-development environment");
            }

            return app;
        }
    }
}
