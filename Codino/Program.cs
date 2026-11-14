
using System;
using System.IO;
using System.Net;

using Codino.Services;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// DEVELOPMENT URL
// =========================================================

if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://127.0.0.1:5088");
}

// =========================================================
// DATA PROTECTION
// =========================================================

var dataProtectionPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "DataProtectionKeys");

Directory.CreateDirectory(dataProtectionPath);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Codino");

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// پشتیبانی از IHttpClientFactory برای کنترلرها
builder.Services.AddHttpClient();

// =========================================================
// FORWARDED HEADERS
// =========================================================

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownProxies.Clear();

    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);

    options.ForwardLimit = 1;
});

// =========================================================
// REDIS CONFIGURATION
// =========================================================

string defaultRedisConnection =
    builder.Environment.IsDevelopment()
        ? "127.0.0.1:7000"
        : "127.0.0.1:6379";

string redisConnection =
    builder.Configuration.GetConnectionString("Redis")
    ?? builder.Configuration["Redis:Configuration"]
    ?? defaultRedisConnection;

string redisInstanceName =
    builder.Configuration["Redis:InstanceName"]
    ?? "Codino:";

redisConnection = redisConnection.Trim();

if (string.IsNullOrWhiteSpace(redisConnection))
{
    redisConnection = defaultRedisConnection;
}

if (string.Equals(
        redisConnection,
        "localhost",
        StringComparison.OrdinalIgnoreCase))
{
    redisConnection = defaultRedisConnection;
}

if (builder.Environment.IsDevelopment()
    && redisConnection.StartsWith(
        "localhost:6379",
        StringComparison.OrdinalIgnoreCase))
{
    redisConnection =
        defaultRedisConnection +
        redisConnection["localhost:6379".Length..];
}

var redisOptions =
    ConfigurationOptions.Parse(redisConnection, true);

redisOptions.AbortOnConnectFail = false;
redisOptions.ConnectTimeout = 10000;
redisOptions.SyncTimeout = 10000;
redisOptions.ConnectRetry = 3;
redisOptions.KeepAlive = 60;

// =========================================================
// REDIS LOGGING
// =========================================================

var redisLogger =
    LoggerFactory.Create(logging =>
    {
        logging.AddConfiguration(
            builder.Configuration.GetSection("Logging"));

        logging.AddConsole();
    }).CreateLogger("Codino.Redis");

redisLogger.LogInformation(
    "Redis configuration loaded. Environment: {Environment}",
    builder.Environment.EnvironmentName);

// رشته اتصال Redis عمداً در لاگ نمایش داده نمی‌شود.

// =========================================================
// REDIS CACHE
// =========================================================

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConfigurationOptions =
        ConfigurationOptions.Parse(redisConnection, true);

    options.ConfigurationOptions.AbortOnConnectFail = false;
    options.ConfigurationOptions.ConnectTimeout = 10000;
    options.ConfigurationOptions.SyncTimeout = 10000;
    options.ConfigurationOptions.ConnectRetry = 3;
    options.ConfigurationOptions.KeepAlive = 60;

    options.InstanceName = redisInstanceName;
});

// =========================================================
// REDIS CONNECTION MULTIPLEXER
// =========================================================

builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var loggerFactory =
        serviceProvider.GetRequiredService<ILoggerFactory>();

    var logger =
        loggerFactory.CreateLogger("Codino.Redis.Connection");

    var options =
        ConfigurationOptions.Parse(redisConnection, true);

    options.AbortOnConnectFail = false;
    options.ConnectTimeout = 10000;
    options.SyncTimeout = 10000;
    options.ConnectRetry = 3;
    options.KeepAlive = 60;

    var multiplexer =
        ConnectionMultiplexer.Connect(options);

    multiplexer.ConnectionFailed += (_, eventArgs) =>
    {
        logger.LogError(
            eventArgs.Exception,
            "Redis connection failed. Endpoint: {Endpoint}",
            eventArgs.EndPoint);
    };

    multiplexer.ConnectionRestored += (_, eventArgs) =>
    {
        logger.LogInformation(
            "Redis connection restored. Endpoint: {Endpoint}",
            eventArgs.EndPoint);
    };

    multiplexer.ErrorMessage += (_, eventArgs) =>
    {
        logger.LogWarning(
            "Redis server message: {Message}",
            eventArgs.Message);
    };

    logger.LogInformation(
        "Redis multiplexer initialized. Connected: {Connected}",
        multiplexer.IsConnected);

    return multiplexer;
});

builder.Services.AddSingleton<RedisRateLimitService>();

// =========================================================
// SESSION
// =========================================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);

    options.Cookie.Name = ".Codino.Session";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
});

// =========================================================
// API BASE URL
// =========================================================

// اولویت با ApiSettings:BaseUrl است.
// تنظیم قدیمی Api:BaseUrl نیز برای سازگاری پشتیبانی می‌شود.

string apiBaseUrl =
    builder.Configuration["ApiSettings:BaseUrl"]
    ?? builder.Configuration["Api:BaseUrl"]
    ?? (builder.Environment.IsDevelopment()
        ? "http://127.0.0.1:5300/"
        : "http://127.0.0.1:5200/");

if (!Uri.TryCreate(
        apiBaseUrl,
        UriKind.Absolute,
        out Uri? apiUri)
    || (apiUri.Scheme != Uri.UriSchemeHttp
        && apiUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "آدرس API در تنظیمات معتبر نیست: " + apiBaseUrl);
}

if (!apiBaseUrl.EndsWith("/", StringComparison.Ordinal))
{
    apiBaseUrl += "/";
}

// کلاینت اختصاصی سرویس فعلی API
builder.Services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// =========================================================
// CODE EXECUTION
// =========================================================

builder.Services.AddScoped<
    ICodeExecutionService,
    DockerCodeExecutionService>();

// =========================================================
// BUILD APPLICATION
// =========================================================

var app = builder.Build();

// =========================================================
// MIDDLEWARE
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// =========================================================
// SECURITY HEADERS
// =========================================================

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";

    headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=(), " +
        "payment=(), usb=(), bluetooth=()";

    headers["X-Permitted-Cross-Domain-Policies"] = "none";

    if (context.Request.Path.StartsWithSegments("/Account"))
    {
        headers["Cache-Control"] =
            "no-store, no-cache, must-revalidate, private";

        headers["Pragma"] = "no-cache";
        headers["Expires"] = "0";
    }

    string connectSrc = app.Environment.IsDevelopment()
        ? "connect-src 'self' http://127.0.0.1:5300 http://localhost:5300;"
        : "connect-src 'self';";

    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'self'; " +
        "form-action 'self'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data: https:; " +
        "style-src 'self' 'unsafe-inline' https:; " +
        "script-src 'self' 'unsafe-inline' https:; " +
        connectSrc + " " +
        "frame-src 'self';";

    if (!app.Environment.IsDevelopment())
    {
        headers["Strict-Transport-Security"] =
            "max-age=31536000; includeSubDomains";
    }

    await next();
});

// =========================================================
// ROUTING
// =========================================================

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================
// STARTUP LOGGING
// =========================================================

app.Logger.LogInformation(
    "Codino MVC is starting. Environment: {Environment}",
    app.Environment.EnvironmentName);

app.Logger.LogInformation(
    "Codino API BaseAddress: {ApiBaseUrl}",
    apiBaseUrl);

app.Run();
