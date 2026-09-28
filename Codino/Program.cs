using Codino.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// MVC
// =========================================================

builder.Services.AddControllersWithViews();

// =========================================================
// SESSION
// =========================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});

// =========================================================
// API HTTP CLIENT
// =========================================================

builder.Services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri(
        "http://127.0.0.1:5200/");
});

// =========================================================
// CODE EXECUTION
// =========================================================

builder.Services.AddScoped<
    ICodeExecutionService,
    DockerCodeExecutionService>();

// =========================================================
// BUILD
// =========================================================

var app = builder.Build();

// =========================================================
// ERROR HANDLING
// =========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

// =========================================================
// HTTPS
// =========================================================

app.UseHttpsRedirection();

// =========================================================
// STATIC FILES
// =========================================================

app.UseStaticFiles();

// =========================================================
// ROUTING
// =========================================================

app.UseRouting();

// =========================================================
// SESSION
// =========================================================

app.UseSession();

// =========================================================
// AUTHORIZATION
// =========================================================

app.UseAuthorization();

// =========================================================
// MVC ROUTE
// =========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================
// RUN MVC
// =========================================================

app.Run("http://127.0.0.1:5000");