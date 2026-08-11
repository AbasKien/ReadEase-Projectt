using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReadEase.Data;
using ReadEase.Helpers;
using ReadEase.Repository;
using ReadEase.Repository.Interfaces;
using ReadEase.Services;
using ReadEase.Services.Interfaces;
using System.Text;

namespace ReadEase.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        // ── Database ──────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(
                config.GetConnectionString("Default"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null)));

        // ── Repositories ──────────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthorApplicationRepository, AuthorApplicationRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookChapterRepository, BookChapterRepository>();
        services.AddScoped<IBookReadRepository, BookReadRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IBookRatingRepository, BookRatingRepository>();
        services.AddScoped<IBookReviewRepository, BookReviewRepository>();
        services.AddScoped<IPostRepository, PostRepository>();

        // ── Services ──────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuthorService, AuthorService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<PasswordHasher>();
        services.AddScoped<ILandingService, LandingService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IChapterService, ChapterService>();

        // ── Authentication: Cookie + Google + JWT ─────────────────────────
        var jwtKey = config["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "Cookies";
            })
            .AddCookie("Cookies", options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/Auth/Login";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
                options.Cookie.MaxAge = null;
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddCookie("External", options =>
            {
                options.Cookie.Name = ".ReadEase.External";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddGoogle(options =>
            {
                options.ClientId = config["Google:ClientId"]!;
                options.ClientSecret = config["Google:ClientSecret"]!;
                options.SignInScheme = "External";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = config["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = config["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // ✅ FIX: Read the JWT from the MobileAuthToken cookie that
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // 1) Try cookie first (WebView in-app requests)
                        if (ctx.Request.Cookies.TryGetValue("MobileAuthToken", out var cookieToken)
                            && !string.IsNullOrWhiteSpace(cookieToken))
                        {
                            // JS stored it with encodeURIComponent — decode it back
                            ctx.Token = Uri.UnescapeDataString(cookieToken);
                        }

                        // 2) Try query string ?token= (browser download redirects from MAUI app)
                        if (string.IsNullOrWhiteSpace(ctx.Token))
                        {
                            var queryToken = ctx.Request.Query["token"].FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(queryToken))
                                ctx.Token = queryToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        // Prevent redirect to login for API routes - return 401 instead
                        ctx.Response.StatusCode = 401;
                        ctx.Response.ContentType = "application/json";
                        return ctx.Response.WriteAsJsonAsync(new { message = "Authentication required." });
                    }
                };
            });

        services.AddAuthorization();

        // ── Swagger ───────────────────────────────────────────────────────
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "ReadEase API",
                Version = "v1",
                Description = "ReadEase Digital Library — JWT + Google OAuth"
            });
            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter your JWT token here"
            });
            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {{
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }});
        });

        // ── CORS ──────────────────────────────────────────────────────────
        services.AddCors(opt =>
            opt.AddPolicy("AllowAll", p =>
                p.SetIsOriginAllowed(_ => true)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials()));

        // ── HTTP Client ───────────────────────────────────────────────────────
        services.AddHttpClient();

        return services;
    }
}