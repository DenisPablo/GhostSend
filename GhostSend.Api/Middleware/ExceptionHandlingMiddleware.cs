using System.Net;
using System.Text.Json;
using GhostSend.Domain.Errors;
using GhostSend.Domain.Exceptions;
using GhostSend.Infrastructure.Persistence;

namespace GhostSend.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);
        }

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            NotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            ValidationException ex => (HttpStatusCode.BadRequest, ex.Message),
            ConflictException ex => (HttpStatusCode.Conflict, ex.Message),
            PersistenceException => (HttpStatusCode.InternalServerError, DomainErrors.General.DatabaseError),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, DomainErrors.General.UnauthorizedAccess),
            _ => (HttpStatusCode.InternalServerError, DomainErrors.General.UnexpectedError)
        };

        response.StatusCode = (int)statusCode;

        var isDevelopment = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

        var result = JsonSerializer.Serialize(new
        {
            error = new
            {
                message = isDevelopment ? exception.Message : message,
                type = exception.GetType().Name,
                layer = exception is BaseException baseEx ? baseEx.Layer : "Unknown",
                correlationId,
                errors = RetrieveErrors(exception),
                technicalMessage = isDevelopment ? exception.InnerException?.Message ?? exception.Message : null,
                stackTrace = isDevelopment ? exception.StackTrace : null
            }
        }, SerializerOptions);

        await response.WriteAsync(result);
    }

    private static object? RetrieveErrors(Exception exception)
    {
        return exception switch
        {
            ValidationException ex => ex.Errors,
            _ => null
        };
    }
}
