using BuildingBlocks.Exceptions.Handler;

namespace Ordering.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApiServices (this IServiceCollection services)
        {


            return services;
        }

        public static WebApplication UseApiServices (this WebApplication app)
        {
            app.UseMiddleware<CustomExceptionHandler>();

            return app;
        }
    }
}
