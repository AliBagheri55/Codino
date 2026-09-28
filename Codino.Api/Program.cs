using Codino.Api.Data;
using Codino.Api.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// CONTROLLERS
// =========================================================

builder.Services.AddControllers();


// =========================================================
// SERVICES
// =========================================================

builder.Services.AddSingleton<PendingRegistrationStore>();


// =========================================================
// DATABASE
// =========================================================

builder.Services.AddDbContext<CodinoDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});


// =========================================================
// REDIS CACHE
// =========================================================

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        "127.0.0.1:7000";

    options.InstanceName =
        "Codino:";
});


// =========================================================
// REDIS CONNECTION MULTIPLEXER
// =========================================================

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        "127.0.0.1:7000"));


// =========================================================
// LOGIN RATE LIMIT
// =========================================================

builder.Services.AddSingleton<
    RedisLoginRateLimitService>();


// =========================================================
// LOGIN ATTEMPT PROTECTION
// =========================================================

builder.Services.AddSingleton<
    RedisLoginAttemptService>();


var app = builder.Build();


// =========================================================
// AUTHORIZATION
// =========================================================

app.UseAuthorization();


// =========================================================
// CONTROLLERS
// =========================================================

app.MapControllers();


// =========================================================
// API STATUS
// =========================================================

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        success = true,
        message = "Codino API is running.",
        address = "127.0.0.1:5200"
    });
});


app.Run(
    "http://127.0.0.1:5200");
