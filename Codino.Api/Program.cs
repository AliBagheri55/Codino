
using System.Text;

using Codino.Api.Data;
using Codino.Api.Services;
using Codino.Api.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// DATABASE
// =========================================================

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection در appsettings.json پیدا نشد.");

builder.Services.AddDbContext<CodinoDbContext>(options =>
{
    options.UseSqlServer(
        connectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
});

// =========================================================
// REDIS CONFIGURATION
// =========================================================

string redisConfiguration =
    builder.Configuration["Redis:Configuration"]
    ?? builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis"]
    ?? "127.0.0.1:6379,abortConnect=false,connectTimeout=10000,syncTimeout=10000,connectRetry=3";

string redisInstanceName =
    builder.Configuration["Redis:InstanceName"]
    ?? "Codino:";

if (string.IsNullOrWhiteSpace(redisConfiguration))
{
    throw new InvalidOperationException(
        "تنظیمات Redis خالی است.");
}

if (string.IsNullOrWhiteSpace(redisInstanceName))
{
    throw new InvalidOperationException(
        "تنظیم Redis:InstanceName خالی است.");
}

ConfigurationOptions redisOptions;

try
{
    redisOptions = ConfigurationOptions.Parse(
        redisConfiguration.Trim(),
        ignoreUnknown: false);

    redisOptions.AbortOnConnectFail = false;
    redisOptions.ConnectTimeout = 10000;
    redisOptions.SyncTimeout = 10000;
    redisOptions.ConnectRetry = 3;
    redisOptions.KeepAlive = 60;
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        "تنظیمات Redis معتبر نیست. " +
        "مقدار Redis:Configuration یا ConnectionStrings:Redis را بررسی کن.",
        ex);
}

builder.Services.AddSingleton<IConnectionMultiplexer>(
    serviceProvider =>
    {
        ILogger<Program> logger =
            serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation(
            "در حال اتصال به Redis. Endpoints: {Endpoints}",
            string.Join(", ", redisOptions.EndPoints));

        return ConnectionMultiplexer.Connect(redisOptions);
    });

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions =
        ConfigurationOptions.Parse(redisConfiguration.Trim());

    options.InstanceName = redisInstanceName;
});

// =========================================================
// JWT SETTINGS
// =========================================================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

JwtSettings jwtSettings =
    builder.Configuration
        .GetSection("Jwt")
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "بخش Jwt در appsettings.json پیدا نشد.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("Jwt:Key خالی است.");
}

byte[] jwtKey = Encoding.UTF8.GetBytes(jwtSettings.Key);

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key باید حداقل 32 بایت باشد.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
{
    throw new InvalidOperationException("Jwt:Issuer خالی است.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("Jwt:Audience خالی است.");
}

if (jwtSettings.AccessTokenMinutes <= 0 ||
    jwtSettings.AccessTokenMinutes > 30)
{
    throw new InvalidOperationException(
        "Jwt:AccessTokenMinutes باید بیشتر از صفر و حداکثر 30 باشد.");
}

if (jwtSettings.RefreshTokenDays <= 0 ||
    jwtSettings.RefreshTokenDays > 30)
{
    throw new InvalidOperationException(
        "Jwt:RefreshTokenDays باید بیشتر از صفر و حداکثر 30 باشد.");
}

// =========================================================
// AUTHENTICATION
// =========================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata =
            !builder.Environment.IsDevelopment();

        options.SaveToken = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),

                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromSeconds(5),

                ValidAlgorithms =
                    new[] { SecurityAlgorithms.HmacSha256 }
            };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                ILogger<Program> logger =
                    context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();

                logger.LogWarning(
                    context.Exception,
                    "JWT authentication failed.");

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse();

                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                return Task.CompletedTask;
            }
        };
    });

// =========================================================
// AUTHORIZATION
// =========================================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
});

// =========================================================
// APPLICATION SERVICES
// =========================================================

builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<RedisLoginRateLimitService>();
builder.Services.AddScoped<RedisLoginAttemptService>();

// =========================================================
// TELEGRAM - LOCAL DEVELOPMENT
// =========================================================

// برای درخواست‌های HTTP به Telegram Bot API
builder.Services.AddHttpClient();

// دریافت پیام‌ها و کلیک دکمه‌ها با Long Polling
builder.Services.AddHostedService<TelegramPollingService>();

// =========================================================
// CORS
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("Codino", policy =>
    {
        policy
            .WithOrigins(
                "https://co-dino.xyz",
                "https://www.co-dino.xyz",
                "http://127.0.0.1:5088",
                "http://localhost:5088")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =========================================================
// CONTROLLERS
// =========================================================

builder.Services.AddControllers();

// =========================================================
// SWAGGER - DEVELOPMENT ONLY
// =========================================================

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "توکن JWT معتبر را وارد کنید."
            });

        options.AddSecurityRequirement(
            document =>
                new OpenApiSecurityRequirement
                {
                    [
                        new OpenApiSecuritySchemeReference(
                            "Bearer",
                            document)
                    ] = []
                });
    });
}

// =========================================================
// BUILD
// =========================================================

var app = builder.Build();

// =========================================================
// REDIS CONNECTION LOGGING
// =========================================================

var redisLogger =
    app.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Codino.Redis");

var redisMultiplexer =
    app.Services.GetRequiredService<IConnectionMultiplexer>();

redisMultiplexer.ConnectionFailed += (_, eventArgs) =>
{
    redisLogger.LogError(
        eventArgs.Exception,
        "اتصال Redis ناموفق شد. Endpoint: {Endpoint}",
        eventArgs.EndPoint);
};

redisMultiplexer.ConnectionRestored += (_, eventArgs) =>
{
    redisLogger.LogInformation(
        "اتصال Redis برقرار شد. Endpoint: {Endpoint}",
        eventArgs.EndPoint);
};

redisLogger.LogInformation(
    "وضعیت اولیه Redis: {Connected}",
    redisMultiplexer.IsConnected);

// =========================================================
// SWAGGER
// =========================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Codino.Api v1");

        options.RoutePrefix = "swagger";
    });
}

// =========================================================
// SECURITY HEADERS
// =========================================================

app.Use(async (context, next) =>
{
    IHeaderDictionary headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";

    headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=(), " +
        "payment=(), usb=(), bluetooth=()";

    headers["Content-Security-Policy"] =
        "default-src 'none'; frame-ancestors 'none';";

    if (!app.Environment.IsDevelopment())
    {
        headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";
    }

    await next();
});

// =========================================================
// MIDDLEWARE
// =========================================================

app.UseCors("Codino");

app.UseAuthentication();
app.UseAuthorization();

// =========================================================
// HEALTH CHECK - API
// =========================================================

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "ok",
        application = "Codino.Api"
    });
})
.AllowAnonymous();

// =========================================================
// HEALTH CHECK - REDIS
// =========================================================

app.MapGet(
    "/health/redis",
    async (IConnectionMultiplexer multiplexer) =>
    {
        try
        {
            TimeSpan latency =
                await multiplexer.GetDatabase().PingAsync();

            return Results.Ok(new
            {
                status = "ok",
                service = "redis",
                connected = true,
                latencyMilliseconds = latency.TotalMilliseconds
            });
        }
        catch (RedisException ex)
        {
            app.Logger.LogError(
                ex,
                "Redis health check failed. IsConnected: {Connected}",
                multiplexer.IsConnected);

            return Results.Json(
                new
                {
                    status = "error",
                    service = "redis",
                    message = "Redis پاسخ نمی‌دهد."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(
                ex,
                "Unexpected Redis health check error.");

            return Results.Json(
                new
                {
                    status = "error",
                    service = "redis",
                    message = "بررسی سلامت Redis با خطا مواجه شد."
                },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .AllowAnonymous();

// =========================================================
// MAP CONTROLLERS
// =========================================================

app.MapControllers();

// =========================================================
// RUN
// =========================================================

app.Run();
