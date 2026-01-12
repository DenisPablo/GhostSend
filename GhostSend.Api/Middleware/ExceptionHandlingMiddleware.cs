using System.Net;
using System.Text.Json;
using GhostSend.Domain.Exceptions;
using GhostSend.Infrastructure.Persistence;

namespace GhostSend.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            NotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            ValidationException ex => (HttpStatusCode.BadRequest, ex.Message),
            ConflictException ex => (HttpStatusCode.Conflict, ex.Message),
            PersistenceException ex => (HttpStatusCode.InternalServerError, "A database error occurred."),
            UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, "Unauthorized access."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        response.StatusCode = (int)statusCode;

        var isDevelopment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        var result = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = isDevelopment ? exception.Message : message,
                type = exception.GetType().Name,
                correlationId = correlationId,
                errors = exception is ValidationException validationEx ? validationEx.Errors : null,
                stackTrace = isDevelopment ? exception.StackTrace : null
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await response.WriteAsync(result);
    }
}
