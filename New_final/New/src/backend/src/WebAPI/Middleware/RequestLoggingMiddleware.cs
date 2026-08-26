using System.Diagnostics;
using System.Text;

namespace WebAPI.Middleware;

/// <summary>
/// Middleware for detailed request/response logging with performance metrics.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;
        var stopwatch = Stopwatch.StartNew();
        
        // Skip logging for certain paths
        var path = context.Request.Path.Value ?? "";
        if (ShouldSkipLogging(path))
        {
            await _next(context);
            return;
        }

        // Log incoming request
        _logger.LogInformation(
            "Request started: {Method} {Path} | CorrelationId: {CorrelationId} | ContentType: {ContentType}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            context.Request.ContentType);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            var level = context.Response.StatusCode >= 400 
                ? LogLevel.Warning 
                : LogLevel.Information;

            _logger.Log(level,
                "Request completed: {Method} {Path} | Status: {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static bool ShouldSkipLogging(string path)
    {
        var skipPaths = new[] { "/health", "/swagger", "/favicon.ico" };
        return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension methods for request logging middleware.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
