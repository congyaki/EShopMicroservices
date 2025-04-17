using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.API.Data.Extensions
{
    public static class DatabaseExtensions
    {
        public static IApplicationBuilder MigrateAuthDatabase(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            try
            {
                // This will apply any pending migrations and create the database if it doesn't exist
                dbContext.Database.Migrate();

                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();
                logger.LogInformation("Database migration completed successfully");
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();
                logger.LogError(ex, "An error occurred while migrating the Auth database");
                throw;
            }

            return app;
        }
    }

}
