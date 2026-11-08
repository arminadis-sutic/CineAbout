using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Interfaces.General;
using EasyNetQ;
using DotNetEnv;
using CineAboutApp.Services.Controller;
using CineAboutApp.Services.Database;
using CineAboutApp.Services.General;
using CineAboutApp.WebAPI.Middleware;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".env")))
{
    Env.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));
}

var builder = WebApplication.CreateBuilder(args);

foreach (var (key, envName) in new[]
{
    ("Jwt:ExpirationInHours", "JWT_EXPIRATION_HOURS"),
    ("Jwt:RefreshExpirationInHours", "JWT_REFRESH_EXPIRATION_HOURS"),
    ("Jwt:ResetExpirationInMinutes", "JWT_RESET_EXPIRATION_MINUTES"),
})
{
    if (!int.TryParse(builder.Configuration[key], out var value) || value <= 0)
        throw new InvalidOperationException($"Configuration value {envName} is missing or not a positive whole number (see .env.example).");
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 268435456;
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 268435456;
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));

    options.AddPolicy("password-reset", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue<int?>("RateLimits:PasswordReset") ?? 8,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0
        }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers.RetryAfter = context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retryAfter)
            ? ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture)
            : "60";

        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please try again later.",
            cancellationToken: token);
    };
});

var connectionString = builder.Configuration.GetConnectionString("db1");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var host = builder.Configuration["DB_HOST"];
    var port = builder.Configuration["DB_PORT"];
    var dbName = builder.Configuration["DB_NAME"];
    var user = builder.Configuration["DB_USER"];
    var pass = builder.Configuration["DB_PASSWORD"];

    connectionString =
        $"Server={host},{port};Database={dbName};User Id={user};Password={pass};TrustServerCertificate=True;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sql => sql.MigrationsAssembly("CineAboutApp.Services")));

builder.Services.AddMapster();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, CacheService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IBus>(
    RabbitHutch.CreateBus(builder.Configuration.GetConnectionString("RabbitMQ")));

builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IUserListService, UserListService>();
builder.Services.AddScoped<IQuizCategoryService, QuizCategoryService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IAwardTypeService, AwardTypeService>();
builder.Services.AddScoped<IPersonService, PersonService>();
builder.Services.AddScoped<IStudioService, StudioService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IQuizQuestionService, QuizQuestionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IThreadService, ThreadService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
builder.Services.AddScoped<IUserNotifier, UserNotifier>();
builder.Services.AddSingleton<ITmdbClient, TmdbClient>();
builder.Services.AddHttpClient(TmdbClient.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<ReminderDispatcherHostedService>();
builder.Services.AddScoped<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
builder.Services.AddScoped<IExportService, ExportService>();

builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

builder.Services.AddScoped<IFileHandler<IFormFile>, FileHandler>();
builder.Services.AddScoped<ITokenHandler<MyAppUser>, MyTokenHandler>();
builder.Services.AddScoped<IRabbitMQHandler, RabbitMQHandler>();

builder.Services.AddSingleton<HtmlToPdfGenerator>();
builder.Services.AddScoped<ReportPdfGenerator>();

builder.Services.AddScoped<IPendingFileCleanup, PendingFileCleanup>();
builder.Services.AddHostedService<PendingFileCleanupHostedService>();
builder.Services.AddHostedService<SignalRBridgeHostedService>();

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 32 * 1024 * 1024;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var security = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        In = ParameterLocation.Header,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT for authorization."
    };

    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        security);

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
            .DefaultInboundClaimTypeMap.Clear();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationsHub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? new[] { "http://localhost:3000", "http://localhost:8080" };

    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retryCount = 5;
    while (retryCount > 0)
    {
        try
        {
            logger.LogInformation("Attempting to connect to database...");
            dbContext.Database.Migrate();
            logger.LogInformation(
                "Database migrations applied successfully to: {Database}",
                dbContext.Database.GetDbConnection().Database);
            break;
        }
        catch (Exception ex)
        {
            retryCount--;
            logger.LogError(
                ex,
                "Database migration failed. Retries left: {RetryCount}",
                retryCount);

            if (retryCount == 0)
                throw;

            await Task.Delay(3000);
        }
    }

    try
    {
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var seedWebRootPath = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        await DataSeeder.SeedAsync(dbContext, passwordHasher, seedWebRootPath);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seeding failed.");
        throw;
    }

    try
    {
       await HtmlToPdfGenerator.InitializeBrowserAsync();
        logger.LogInformation("Puppeteer Chromium binary pre-initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to pre-initialize Puppeteer Chromium binary");
    }
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRouting();
app.UseCors("ApiPolicy");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var webRootPath = builder.Environment.WebRootPath ??
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

if (!Directory.Exists(webRootPath))
    Directory.CreateDirectory(webRootPath);

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/media/" + ProfileImages.Folder, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(webRootPath),
    RequestPath = "/media",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.Append(
            "Access-Control-Allow-Origin", "*");
        context.Context.Response.Headers.Append(
            "Access-Control-Allow-Methods", "GET, HEAD, OPTIONS");
    }
});

app.UseRateLimiter();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();

app.UseAuthorization();

app.MapHub<CineAboutApp.Services.Hubs.NotificationHub>("/notificationsHub");
app.MapControllers();

app.Run();