namespace ReadEase.Middleware;

/// <summary>
/// Middleware that extracts JWT token from query string and adds it to Authorization header.
/// This allows browser downloads to work with token in URL: ?token=xyz
/// </summary>
public class QueryTokenAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<QueryTokenAuthMiddleware> _logger;

    public QueryTokenAuthMiddleware(RequestDelegate next, ILogger<QueryTokenAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if Authorization header is missing or empty
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
        {
            // Try to get token from query string
            var token = context.Request.Query["token"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(token))
            {
                // Add Bearer token to Authorization header
                context.Request.Headers.Authorization = $"Bearer {token}";
                _logger.LogDebug("Added Bearer token from query string for path: {Path}", context.Request.Path);
            }
        }

        await _next(context);
    }
}
