using System.Net;
using System.Text.Json;
using FluentValidation;
using Serilog;

namespace WebAPI.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions and returns standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        // Log the exception with correlation ID for tracing
        _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

        var response = context.Response;
        response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            ValidationException validationException => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Instance = context.Request.Path,
                CorrelationId = correlationId,
                Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            },
            
            ArgumentNullException argNullEx => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Argument",
                Detail = $"The argument '{argNullEx.ParamName}' cannot be null.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            ArgumentException argEx => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Invalid Argument",
                Detail = argEx.Message,
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            KeyNotFoundException => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Resource Not Found",
                Detail = "The requested resource was not found.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            UnauthorizedAccessException => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = "You are not authorized to access this resource.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            InvalidOperationException invalidOpEx => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Invalid Operation",
                Detail = invalidOpEx.Message,
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            TaskCanceledException or OperationCanceledException => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.RequestTimeout,
                Title = "Request Timeout",
                Detail = "The operation was cancelled or timed out.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            HttpRequestException httpEx => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.BadGateway,
                Title = "External Service Error",
                Detail = "An error occurred while communicating with an external service.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            },
            
            _ => new ProblemDetailsResponse
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path,
                CorrelationId = correlationId
            }
        };

        response.StatusCode = problemDetails.Status;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await response.WriteAsJsonAsync(problemDetails, options);
    }
}

/// <summary>
/// Problem details response following RFC 7807 standard with extensions.
/// </summary>
public class ProblemDetailsResponse
{
    public string Type { get; set; } = "about:blank";
    public string Title { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Extension methods for adding exception handling middleware.
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
