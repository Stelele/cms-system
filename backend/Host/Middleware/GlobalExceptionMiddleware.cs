using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Host.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response = exception switch
        {
            ValidationException validationEx => new
            {
                error = new
                {
                    message = "Validation failed",
                    type = "ValidationException",
                    errors = validationEx.Errors.Select(e => new
                    {
                        property = e.PropertyName,
                        message = e.ErrorMessage
                    })
                }
            },
            JsonException => new
            {
                error = new
                {
                    message = "Invalid JSON format or missing required fields",
                    type = exception.GetType().Name
                }
            },
            DbUpdateException => new
            {
                error = new
                {
                    message = "A blog with this name or slug already exists.",
                    type = "DuplicateKeyException",
                    constraint = DetectConstraint(exception)
                }
            },
            _ => new
            {
                error = new
                {
                    message = exception.Message,
                    type = exception.GetType().Name
                }
            }
        };

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            JsonException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            DbUpdateException => (int)HttpStatusCode.Conflict,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        await context.Response.WriteAsync(jsonResponse);
    }

    private static string DetectConstraint(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        if (message.Contains("Name", StringComparison.OrdinalIgnoreCase)) return "Name";
        if (message.Contains("Slug", StringComparison.OrdinalIgnoreCase)) return "Slug";
        return "Unknown";
    }
}
