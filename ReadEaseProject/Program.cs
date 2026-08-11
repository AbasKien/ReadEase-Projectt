using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReadEase.Data;
using ReadEase.Enums;
using ReadEase.Extensions;
using ReadEase.Hubs;
using ReadEase.Jobs;
using ReadEase.Middleware;
using ReadEase.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Register services ─────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});
builder.Services.AddApplicationServices(builder.Configuration);

// ── SignalR ───────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Hangfire ──────────────────────────────────────────────────────────────
var hangfireConnStr = builder.Configuration.GetConnectionString("Hangfire");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnStr, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;   // keep low — single-server app
});

// Register the job so Hangfire can DI-resolve it
builder.Services.AddScoped<ChapterReleaseJob>();

// Add Session for ExternalLogin mobile flag
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Seed admin user on first run ──────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Users.AnyAsync(u => u.Email == "admin@readease.com"))
    {
        var admin = new User
        {
            Name = "System Admin",
            Email = "admin@readease.com",
            Role = UserRole.Admin,
            Provider = "Local",
            IsEmailConfirmed = true,
            IsActive = true,
            SubscriptionTier = SubscriptionTier.PremiumPlus,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, "Admin@123");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

// ── Middleware pipeline (ORDER MATTERS) ───────────────────────────────────

// Trust forwarded headers from ngrok/reverse proxies so Google OAuth
// generates the correct public callback URL (not localhost)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
                     | ForwardedHeaders.XForwardedHost
});

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReadEase API v1");
        c.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();
// Extract token from query string for browser downloads (must be before Authentication)
app.UseMiddleware<QueryTokenAuthMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Prevent redirect to login for API routes - return 401 instead
app.Use(async (context, next) =>
{
    var isApi = context.Request.Path.StartsWithSegments("/Areas/Mobile/api") ||
                context.Request.Path.StartsWithSegments("/api");
    
    if (isApi)
    {
        var originalBody = context.Response.Body;
        
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;
        
        await next();
        
        if (context.Response.StatusCode == 302 || context.Response.StatusCode == 301)
        {
            var location = context.Response.Headers.Location.ToString();
            if (location.Contains("/Auth/Login"))
            {
                // Return 401 for API authentication failures
                context.Response.StatusCode = 401;
                context.Response.Body = originalBody;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Remove("Location");
                await context.Response.WriteAsJsonAsync(new { message = "Authentication required." });
                return;
            }
        }
        
        memoryStream.Seek(0, SeekOrigin.Begin);
        context.Response.Body = originalBody;
        await memoryStream.CopyToAsync(context.Response.Body);
    }
    else
    {
        await next();
    }
});

// ── Hangfire Dashboard (Admin only) ──────────────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});

// ── Routes ────────────────────────────────────────────────────────────────
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Landing}/{id?}");

app.MapControllers();

// ── SignalR Hub endpoints ─────────────────────────────────────────────────
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ReadEase.Hubs.AuditHub>("/hubs/audit");

app.Run();