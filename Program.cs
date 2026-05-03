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

    Console.WriteLine($"[DB-INIT] Path={dbPath} Exists={File.Exists(dbPath)}");

    var found = db.Database.GetMigrations().ToList();
    Console.WriteLine($"[DB-INIT] Migrations in assembly ({found.Count}): {string.Join(", ", found)}");

    if (db.Database.CanConnect())
    {
        var applied = db.Database.GetAppliedMigrations().ToList();
        Console.WriteLine($"[DB-INIT] Applied ({applied.Count}): {string.Join(", ", applied)}");
        if (!applied.Any())
        {
            Console.WriteLine("[DB-INIT] DB exists but no migrations applied - deleting for fresh start");
            db.Database.EnsureDeleted();
        }
    }

    try
    {
        db.Database.Migrate();
        var afterApplied = db.Database.GetAppliedMigrations().ToList();
        Console.WriteLine($"[DB-INIT] Migrate done. Applied now ({afterApplied.Count}): {string.Join(", ", afterApplied)}");
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name";
        using var reader = cmd.ExecuteReader();
        var tables = new List<string>();
        while (reader.Read()) tables.Add(reader.GetString(0));
        Console.WriteLine($"[DB-INIT] Tables ({tables.Count}): {string.Join(", ", tables)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB-INIT] Migrate FAILED: {ex.GetType().Name}: {ex.Message}");
        throw;
    }
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
