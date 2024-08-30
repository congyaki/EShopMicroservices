using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Exceptions.Handler
{
    public record ExceptionResponse(string Detail, string Title, int StatusCode);
    public class CustomExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomExceptionHandler> _logger;

        public CustomExceptionHandler(RequestDelegate next, ILogger<CustomExceptionHandler> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, new CancellationToken());
            }
        }

        public async ValueTask<bool> HandleExceptionAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError("Error Message: {exceptionMessage}, Time of occurrence {time}",
            exception.Message, DateTime.UtcNow);

            //More log stuff        

            (string Detail, string Title, int StatusCode) details = exception switch
            {
                InternalServerException => 
                (
                    exception.Message, 
                    exception.GetType().Name, 
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError
                    ),
                ValidationException => 
                (
                    exception.Message, 
                    exception.GetType().Name, 
                    context.Response.StatusCode = StatusCodes.Status400BadRequest
                    ),
                BadRequestException => 
                (
                    exception.Message, 
                    exception.GetType().Name, 
                    context.Response.StatusCode = StatusCodes.Status400BadRequest
                    ),
                NotFoundException => 
                (
                    exception.Message, 
                    exception.GetType().Name, 
                    context.Response.StatusCode = StatusCodes.Status404NotFound
                    ),
                _ => 
                (
                    exception.Message, 
                    exception.GetType().Name, 
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError
                    )
            };

            var problemDetails = new ProblemDetails
            {
                Title = details.Title,
                Detail = details.Detail,
                Status = details.StatusCode,
                Instance = context.Request.Path
            };

            problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

            if (exception is ValidationException validationException)
            {
                problemDetails.Extensions.Add("ValidationErrors", validationException.Errors);
            }
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)details.StatusCode;
            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
