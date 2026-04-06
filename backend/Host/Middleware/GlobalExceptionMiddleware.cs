using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Host.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next)
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorType, message, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "ValidationException",
                "Validation failed",
                validationException.Errors.Select(e => new Error(e.ErrorMessage, e.PropertyName, [])).ToList()
            ),
            DbUpdateException => (
                StatusCodes.Status400BadRequest,
                "DbUpdateException",
                "Database update failed",
                []
            ),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "InvalidOperationException",
                exception.Message,
                []
            ),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "KeyNotFoundException",
                exception.Message,
                []
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "UnauthorizedAccessException",
                "Unauthorized",
                []
            ),
            SecurityTokenExpiredException => (
                StatusCodes.Status401Unauthorized,
                "SecurityTokenExpiredException",
                "Token has expired",
                []
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Exception",
                "An unexpected error occurred",
                []
            )
        };

        context.Response.StatusCode = statusCode;

        var errorResponse = new ErrorResponse(new Error(message, errorType, errors));

        var json = JsonSerializer.Serialize(errorResponse, JsonSerializerOptions.Web);
        await context.Response.WriteAsync(json);
    }
}

public sealed record Error(string Message, string Type, List<Error> Errors);

public sealed record ErrorResponse(Error Error);