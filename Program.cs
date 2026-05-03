using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Progetto_Web_2_IoT_Auth.Components;
using Progetto_Web_2_IoT_Auth.Data;
using Progetto_Web_2_IoT_Auth.Endpoints;
using Progetto_Web_2_IoT_Auth.Services;


var builder = WebApplication.CreateBuilder(args);

var dbFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
    ? "/home/data"
    : Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "db.sqlite");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddDbContext<DbContextSQLite>(options => options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = "auth_token";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddHostedService<AutomationEngineService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();

    // If the DB exists but has no applied migrations it is in a broken state
    // (created by a previous deployment before migrations were wired up).
    // Delete it so Migrate() can build a clean schema from scratch.
    if (db.Database.CanConnect() && !db.Database.GetAppliedMigrations().Any())
        db.Database.EnsureDeleted();

    db.Database.Migrate();
}

app.UseStatusCodePagesWithReExecute("/not-found");

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapAuthEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
