using Microsoft.EntityFrameworkCore;
using Progetto_Web_2_IoT_Auth.Data;

namespace Progetto_Web_2_IoT_Auth.Services;

public interface IAutomationEngineService
{
    Task RunTickAsync(CancellationToken ct);
}

public class AutomationEngineService : BackgroundService, IAutomationEngineService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomationEngineService> _logger;

    public AutomationEngineService(IServiceScopeFactory scopeFactory, ILogger<AutomationEngineService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunTickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automation engine tick failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    public async Task RunTickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
        var weather = scope.ServiceProvider.GetRequiredService<IWeatherService>();

        var automations = await db.Automations
            .AsNoTracking()
            .Include(a => a.Device)
            .ToListAsync(ct);

        if (automations.Count == 0)
            return;

        var currentTime = DateTime.Now.ToString("HH:mm");

        // Only call the weather API once per tick, and only if needed
        bool? isRaining = null;
        if (automations.Any(a => !string.IsNullOrWhiteSpace(a.WeatherCondition)))
        {
            try
            {
                isRaining = await weather.IsRainingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Weather check failed, skipping weather-based automations");
            }
        }

        foreach (var auto in automations)
        {
            if (!MatchesTime(auto.TimeCondition, currentTime))
                continue;

            if (!MatchesWeather(auto.WeatherCondition, isRaining))
                continue;

            // Skip if device is already in the target state
            if (auto.Device.Power == auto.Power && auto.Device.Level == auto.Level)
                continue;

            await db.Devices
                .Where(d => d.Id == auto.DeviceId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Power, auto.Power)
                    .SetProperty(d => d.Level, auto.Level), ct);

            _logger.LogInformation(
                "Automation {Id} applied: device '{Device}' -> Power={Power}, Level={Level}",
                auto.Id, auto.Device.Name, auto.Power, auto.Level);
        }
    }

    private static bool MatchesTime(string timeCondition, string currentTime)
    {
        if (string.IsNullOrWhiteSpace(timeCondition))
            return true; // no time constraint

        return string.Equals(timeCondition.Trim(), currentTime, StringComparison.Ordinal);
    }

    private static bool MatchesWeather(string weatherCondition, bool? isRaining)
    {
        if (string.IsNullOrWhiteSpace(weatherCondition))
            return true; // no weather constraint

        // Weather API failed — don't trigger weather-based automations
        if (isRaining is null)
            return false;

        return weatherCondition.Trim().ToLowerInvariant() switch
        {
            "rain" => isRaining.Value,
            "no_rain" => !isRaining.Value,
            _ => false
        };
    }
}
