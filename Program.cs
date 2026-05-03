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
    var log = app.Logger;

    log.LogInformation("[DB-INIT] Path={Path} Exists={Exists}", dbPath, File.Exists(dbPath));

    // Check whether the schema is already in place by looking for one of our tables.
    bool schemaReady = false;
    if (db.Database.CanConnect())
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='automation'";
        schemaReady = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        log.LogInformation("[DB-INIT] schemaReady={SchemaReady}", schemaReady);
    }

    if (!schemaReady)
    {
        log.LogInformation("[DB-INIT] Building schema from model via EnsureCreated");
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        log.LogInformation("[DB-INIT] EnsureCreated complete");
    }

    // Azure Files (the mount under /home on App Service Linux) doesn't support
    // SQLite's WAL shared-memory locking, so force the DB into DELETE journal mode.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
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
