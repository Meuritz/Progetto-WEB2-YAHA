using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Progetto_Web_2_IoT_Auth.Data;

namespace Progetto_Web_2_IoT_Auth.Services;

public interface ILoginService
{
    Task<LoginResult> AuthenticateAsync(string name, string password);
    string GenerateToken(int userId, string name, string role);
    void SetAuthCookie(HttpContext context, string token, IWebHostEnvironment environment, bool rememberMe);
}

public class LoginService : ILoginService
{
    private readonly DbContextSQLite _context;
    private readonly ILogger<LoginService> _logger;

    public LoginService(DbContextSQLite context, ILogger<LoginService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LoginResult> AuthenticateAsync(string name, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
                return LoginResult.Failure("name and password are required");

            var username = name.Trim();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid login for {Name}", username);
                return LoginResult.Failure("Invalid name or password");
            }

            var role = string.IsNullOrWhiteSpace(user.Role) ? "user" : user.Role.Trim().ToLowerInvariant();
            _logger.LogInformation("Login ok for {name} (ID {Id}, Role {Role})", username, user.Id, role);
            return LoginResult.Success(user.Id, user.Username, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auth error for {Name}", name);
            return LoginResult.Failure("Authentication error");
        }
    }

    public string GenerateToken(int userId, string name, string role)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tokenData = $"{userId}:{name}:{role}:{ts}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData));
    }

    public void SetAuthCookie(HttpContext context, string token, IWebHostEnvironment environment, bool rememberMe)
    {
        var opts = new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };
        if (rememberMe)
            opts.Expires = DateTimeOffset.UtcNow.AddDays(7);

        context.Response.Cookies.Append("auth_token", token, opts);
        _logger.LogDebug("Auth cookie set (rememberMe={Remember})", rememberMe);
    }
}

public class LoginResult
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; } = string.Empty;
    public int UserId { get; }
    public string Name { get; } = string.Empty;
    public string Role { get; } = string.Empty;

    private LoginResult(bool ok, string error, int id, string name, string role)
    {
        IsSuccess = ok;
        ErrorMessage = error;
        UserId = id;
        Name = name;
        Role = role;
    }

    public static LoginResult Success(int id, string name, string role) => new(true, string.Empty, id, name, role);
    public static LoginResult Failure(string error) => new(false, error, 0, string.Empty, string.Empty);
}