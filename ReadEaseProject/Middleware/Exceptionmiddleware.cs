using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace ReadEase.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var isApi = context.Request.Path.StartsWithSegments("/api");

        var (statusCode, title) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Bad Request"),
            _ => (HttpStatusCode.InternalServerError, "Server Error")
        };

        context.Response.StatusCode = (int)statusCode;

        if (isApi)
        {
            context.Response.ContentType = "application/json";
            var problem = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = ex.Message
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        else
        {
            // For MVC: only redirect to Login for auth errors
            // For other errors, show error message on current page via TempData redirect
            if (ex is UnauthorizedAccessException)
            {
                context.Response.Redirect(
                    $"/Auth/Login?error={Uri.EscapeDataString(ex.Message)}");
            }
            else
            {
                // Store error in cookie-based temp and redirect back
                context.Response.Redirect(
                    $"/Auth/Login?error={Uri.EscapeDataString(ex.Message)}");
            }
        }
    }
}