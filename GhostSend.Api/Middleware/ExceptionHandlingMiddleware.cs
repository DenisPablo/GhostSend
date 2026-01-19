using System.Net;
using System.Text.Json;
using GhostSend.Application.Common.Exceptions;
using GhostSend.Domain.Errors;
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
            GhostSend.Domain.Exceptions.ValidationException ex => (HttpStatusCode.BadRequest, ex.Message),
            GhostSend.Application.Common.Exceptions.ValidationException ex => (HttpStatusCode.BadRequest, ex.Message),
            ConflictException ex => (HttpStatusCode.Conflict, ex.Message),
            ForbiddenAccessException ex => (HttpStatusCode.Forbidden, ex.Message),
            PersistenceException ex => (HttpStatusCode.InternalServerError, DomainErrors.General.DatabaseError),
            UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, DomainErrors.General.UnauthorizedAccess),
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
                correlationId = correlationId,
                errors = RetrieveErrors(exception),
                stackTrace = isDevelopment ? exception.StackTrace : null
            }
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await response.WriteAsync(result);
    }

    private static object? RetrieveErrors(Exception exception)
    {
        return exception switch
        {
            GhostSend.Domain.Exceptions.ValidationException ex => ex.Errors,
            GhostSend.Application.Common.Exceptions.ValidationException ex => ex.Errors,
            _ => null
        };
    }
}
