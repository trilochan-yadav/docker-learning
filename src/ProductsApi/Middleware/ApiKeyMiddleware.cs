namespace ProductsApi.Middleware;

/// <summary>
/// Middleware that authenticates every request using an API key supplied in
/// the <c>X-Api-Key</c> HTTP header.
///
/// Swagger static asset paths are exempt so the UI loads without authentication,
/// but every <c>/api/...</c> call made from within Swagger still requires the key
/// (entered once via the Authorize / padlock dialog).
///
/// Returns 401 with a JSON error body for missing or invalid keys.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    // Paths that are allowed through without an API key
    // so the Swagger UI assets load correctly in the browser
    private static readonly string[] PublicPathPrefixes =
    [
        "/swagger"
    ];

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Allow Swagger UI asset requests through without an API key
        if (PublicPathPrefixes.Any(prefix =>
                path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Read the configured API key (supports override via environment variable:
        // ApiSettings__ApiKey=<value> in docker-compose or hosting environment)
        var configuredKey = _configuration["ApiSettings:ApiKey"];

        // Extract and validate the key from the request header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey)
            || !string.Equals(providedKey, configuredKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"statusCode\":401,\"message\":\"Invalid or missing API key.\"}");
            return;
        }

        await _next(context);
    }
}
