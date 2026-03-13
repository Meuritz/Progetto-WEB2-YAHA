using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Progetto_Web_2_IoT_Auth.Components;
using Progetto_Web_2_IoT_Auth.Data;
using Progetto_Web_2_IoT_Auth.Endpoints;
using Progetto_Web_2_IoT_Auth.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddDbContext<DbContextSQLite>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevDbSeed");

    try
    {
        await db.Database.MigrateAsync();

        const string oldSeededAdminHash = "$2a$11$wBHBpKnudJZ9U1yNZT5l5u4xGkVmI0QRjvEm0ZQ76C8/3Ha7jKOaC";
        const string newSeededAdminHash = "$2a$11$5bXqGaqh3uehFVuTEdfWLOfFUxE7KFIRYv/XOqmEgdon7oNxpVQxS";

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin != null && admin.PasswordHash == oldSeededAdminHash)
        {
            admin.PasswordHash = newSeededAdminHash;
            await db.SaveChangesAsync();
            log.LogInformation("Updated default admin password hash in existing dev database (username=admin, password=admin).");
        }

        var adminIds = await db.Users
            .Where(u => u.Role == "admin")
            .Select(u => u.Id)
            .ToListAsync();

        if (adminIds.Count > 0)
        {
            var invalidAccessRows = await db.Accesses
                .Where(a => adminIds.Contains(a.UserId) || a.AccessLevel == "admin")
                .ToListAsync();

            if (invalidAccessRows.Count > 0)
            {
                db.Accesses.RemoveRange(invalidAccessRows);
                await db.SaveChangesAsync();
                log.LogInformation("Removed {Count} invalid access rows for admin users.", invalidAccessRows.Count);
            }
        }
    }
    catch (Exception ex)
    {
        log.LogError(ex, "Dev database initialization failed");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapAuthEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
