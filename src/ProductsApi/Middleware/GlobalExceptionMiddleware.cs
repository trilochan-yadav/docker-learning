using System.Net;
using System.Text.Json;

namespace ProductsApi.Middleware;

/// <summary>
/// Outermost middleware that catches all unhandled exceptions in the pipeline
/// and converts them to consistent JSON error responses.
///
/// Registration order in Program.cs: this must be the FIRST middleware added
/// so it wraps every subsequent middleware and the controller layer.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    // Shared serializer options — camelCase to match the rest of the API
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Map exception types to HTTP status codes.
        // Business exceptions (404/400) are already logged as Warnings in the BLL
        // where they originate — logging them again here at Error level would be
        // misleading (Error implies unexpected) and create duplicate Seq entries.
        var (statusCode, message) = ex switch
        {
            KeyNotFoundException knf => (HttpStatusCode.NotFound, knf.Message),
            ArgumentException ae => (HttpStatusCode.BadRequest, ae.Message),
            InvalidOperationException i => (HttpStatusCode.BadRequest, i.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        // Only log at Error for truly unexpected exceptions (500s).
        // These have no other log entry — the stack trace here is the sole diagnostic signal.
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Write a consistent error envelope to the response body
        var payload = JsonSerializer.Serialize(
            new { statusCode = (int)statusCode, message },
            SerializerOptions);

        await context.Response.WriteAsync(payload);
    }
}
