using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Progetto_Web_2_IoT_Auth.Data;
using System.Security.Claims;

namespace Progetto_Web_2_IoT_Auth.Services;

public class UserAccessService
{
    private readonly DbContextSQLite _db;
    private readonly AuthenticationStateProvider _authStateProvider;

    public UserAccessService(DbContextSQLite db, AuthenticationStateProvider authStateProvider)
    {
        _db = db;
        _authStateProvider = authStateProvider;
    }

    public async Task<UserAccessInfo> GetAccessInfoAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        var isAdmin = user.IsInRole("admin");

        var idRaw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(idRaw, out var id) ? id : null;

        var accessByZone = new Dictionary<int, string>();

        if (!isAdmin && userId is int uid)
        {
            var accesses = await _db.Accesses
                .AsNoTracking()
                .Where(a => a.UserId == uid && (a.AccessLevel == "view" || a.AccessLevel == "operator"))
                .ToListAsync();

            foreach (var a in accesses)
            {
                if (!accessByZone.ContainsKey(a.ZoneId))
                    accessByZone[a.ZoneId] = (a.AccessLevel ?? string.Empty).Trim().ToLowerInvariant();
            }
        }

        return new UserAccessInfo
        {
            UserId = userId,
            IsAdmin = isAdmin,
            AccessLevelByZoneId = accessByZone
        };
    }
}

public class UserAccessInfo
{
    public int? UserId { get; init; }
    public bool IsAdmin { get; init; }
    public Dictionary<int, string> AccessLevelByZoneId { get; init; } = new();

    public bool CanOperateZone(int zoneId)
        => IsAdmin || (AccessLevelByZoneId.TryGetValue(zoneId, out var level) && level == "operator");

    public bool CanViewZone(int zoneId)
        => IsAdmin || AccessLevelByZoneId.ContainsKey(zoneId);

    public bool CanOperateAny
        => IsAdmin || AccessLevelByZoneId.Values.Any(l => l == "operator");

    public List<int> AccessibleZoneIds
        => IsAdmin ? new List<int>() : AccessLevelByZoneId.Keys.ToList();

    public List<int> OperatorZoneIds
        => IsAdmin
            ? new List<int>()
            : AccessLevelByZoneId.Where(kv => kv.Value == "operator").Select(kv => kv.Key).ToList();
}
