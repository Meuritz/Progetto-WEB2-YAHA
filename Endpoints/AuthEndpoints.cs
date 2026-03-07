using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Progetto_Web_2_IoT_Auth.Services;

namespace Progetto_Web_2_IoT_Auth.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/login-action", HandleLoginAction);
        app.MapPost("/logout-action", HandleLogoutAction);
    }

    private static async Task HandleLoginAction(
        HttpContext context,
        ILoginService loginService,
        IWebHostEnvironment env,
        ILogger<Program> logger)
    {
        try
        {
            var form = await context.Request.ReadFormAsync();
            var name = (form["name"].ToString() ?? string.Empty).Trim();
            var password = form["password"].ToString();

            var rememberMeRaw = form["rememberMe"].ToString();
            var rememberMe = string.Equals(rememberMeRaw, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rememberMeRaw, "on", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rememberMeRaw, "1", StringComparison.OrdinalIgnoreCase);

            var returnUrl = form["ReturnUrl"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = context.Request.Query["ReturnUrl"].ToString();

            static bool IsSafeLocalReturnUrl(string? url)
                => !string.IsNullOrWhiteSpace(url)
                   && Uri.IsWellFormedUriString(url, UriKind.Relative)
                   && url.StartsWith('/')
                   && !url.StartsWith("//", StringComparison.Ordinal);

            var result = await loginService.AuthenticateAsync(name, password);
            if (!result.IsSuccess)
            {
                var url = "/login?error=" + Uri.EscapeDataString(result.ErrorMessage);
                if (IsSafeLocalReturnUrl(returnUrl))
                    url += "&ReturnUrl=" + Uri.EscapeDataString(returnUrl);

                context.Response.Redirect(url);
                return;
            }

            // Create claims and sign in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.Name),
                new Claim(ClaimTypes.Role, result.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            logger.LogInformation("Login for {Name} role={Role}", result.Name, result.Role);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = rememberMe
            };

            if (rememberMe)
                authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            context.Response.Redirect(IsSafeLocalReturnUrl(returnUrl) ? returnUrl! : "/");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login action error");
            context.Response.Redirect("/login?error=" + Uri.EscapeDataString("Unexpected error"));
        }
    }

    private static async Task HandleLogoutAction(HttpContext context, ILogger<Program> logger)
    {
        try
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            logger.LogInformation("Logout");
            context.Response.Redirect("/logout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Logout error");
            context.Response.Redirect("/login?error=" + Uri.EscapeDataString("Logout error"));
        }
    }
}