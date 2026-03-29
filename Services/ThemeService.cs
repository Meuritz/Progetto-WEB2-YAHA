using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Progetto_Web_2_IoT_Auth.Data;

namespace Progetto_Web_2_IoT_Auth.Services;

public interface IThemeService
{
    Task EnsureLoadedAsync();
    Task SetDarkModeAsync(bool value);
    Task ToggleAsync();
}

public sealed class ThemeService : IThemeService
{
    private readonly DbContextSQLite _db;
    private readonly AuthenticationStateProvider _authStateProvider;

    private int? _loadedForUserId;

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public ThemeService(DbContextSQLite db, AuthenticationStateProvider authStateProvider)
    {
        _db = db;
        _authStateProvider = authStateProvider;
    }

    public async Task EnsureLoadedAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId is null)
        {
            if (_loadedForUserId is not null)
            {
                _loadedForUserId = null;
                IsDarkMode = false;
                OnChange?.Invoke();
            }

            return;
        }

        if (_loadedForUserId == userId)
            return;

        _loadedForUserId = userId;

        IsDarkMode = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.DarkMode)
            .FirstOrDefaultAsync();

        OnChange?.Invoke();
    }

    public async Task SetDarkModeAsync(bool value)
    {
        if (IsDarkMode == value)
            return;

        IsDarkMode = value;

        var userId = await GetCurrentUserIdAsync();
        if (userId is int uid)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == uid);
            if (user is not null)
            {
                user.DarkMode = value;
                await _db.SaveChangesAsync();
            }
        }

        OnChange?.Invoke();
    }

    public Task ToggleAsync() => SetDarkModeAsync(!IsDarkMode);

    private async Task<int?> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var idRaw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idRaw, out var id) ? id : null;
    }
}
