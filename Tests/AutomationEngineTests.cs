using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Progetto_Web_2_IoT_Auth.Data;
using Progetto_Web_2_IoT_Auth.Data.Model;
using Progetto_Web_2_IoT_Auth.Services;
using Xunit;

namespace Progetto_Web_2_IoT_Auth.Tests;

public class AutomationEngineTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    public AutomationEngineTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<DbContextSQLite>(o => o.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
        db.Database.EnsureCreated();

        // Wipe seed data so tests start from a clean state
        db.Automations.RemoveRange(db.Automations);
        db.Devices.RemoveRange(db.Devices);
        db.Accesses.RemoveRange(db.Accesses);
        db.Users.RemoveRange(db.Users);
        db.SaveChanges();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

    private AutomationEngineService CreateEngine(IWeatherService weather)
    {
        var scopeFactory = new ScopedFactoryWithWeather(_provider, weather);
        return new AutomationEngineService(scopeFactory, NullLogger<AutomationEngineService>.Instance);
    }

    private async Task<int> AddDeviceAsync(bool power, int level)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
        var device = new Device
        {
            Name = "test",
            ZoneId = 1,
            DeviceTypeId = 1,
            IpAddress = "127.0.0.1",
            Power = power,
            Level = level
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        return device.Id;
    }

    private async Task AddAutomationAsync(int deviceId, bool power, int level, string time, string weather)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
        db.Automations.Add(new Automation
        {
            DeviceId = deviceId,
            Power = power,
            Level = level,
            TimeCondition = time,
            WeatherCondition = weather
        });
        await db.SaveChangesAsync();
    }

    private async Task<Device> ReloadDeviceAsync(int deviceId)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContextSQLite>();
        return await db.Devices.AsNoTracking().FirstAsync(d => d.Id == deviceId);
    }

    [Fact]
    public async Task NoAutomations_DoesNothing()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.False(d.Power);
        Assert.Equal(0, d.Level);
    }

    [Fact]
    public async Task AutomationWithoutConditions_AlwaysApplied()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        await AddAutomationAsync(deviceId, power: true, level: 75, time: string.Empty, weather: string.Empty);

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.True(d.Power);
        Assert.Equal(75, d.Level);
    }

    [Fact]
    public async Task AutomationWithMatchingTime_Applied()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        var now = DateTime.Now.ToString("HH:mm");
        await AddAutomationAsync(deviceId, power: true, level: 50, time: now, weather: string.Empty);

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.True(d.Power);
        Assert.Equal(50, d.Level);
    }

    [Fact]
    public async Task AutomationWithNonMatchingTime_NotApplied()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        var nonMatching = DateTime.Now.AddMinutes(-5).ToString("HH:mm");
        if (nonMatching == DateTime.Now.ToString("HH:mm"))
            nonMatching = DateTime.Now.AddMinutes(-10).ToString("HH:mm");

        await AddAutomationAsync(deviceId, power: true, level: 99, time: nonMatching, weather: string.Empty);

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.False(d.Power);
        Assert.Equal(0, d.Level);
    }

    [Fact]
    public async Task RainAutomation_AppliedWhenRaining()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        await AddAutomationAsync(deviceId, power: true, level: 30, time: string.Empty, weather: "rain");

        var engine = CreateEngine(new FakeWeatherService(true));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.True(d.Power);
        Assert.Equal(30, d.Level);
    }

    [Fact]
    public async Task RainAutomation_NotAppliedWhenSunny()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        await AddAutomationAsync(deviceId, power: true, level: 30, time: string.Empty, weather: "rain");

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.False(d.Power);
        Assert.Equal(0, d.Level);
    }

    [Fact]
    public async Task NoRainAutomation_AppliedWhenSunny()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        await AddAutomationAsync(deviceId, power: true, level: 60, time: string.Empty, weather: "no_rain");

        var engine = CreateEngine(new FakeWeatherService(false));
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.True(d.Power);
        Assert.Equal(60, d.Level);
    }

    [Fact]
    public async Task WeatherAutomation_SkippedWhenWeatherFails()
    {
        var deviceId = await AddDeviceAsync(power: false, level: 0);
        await AddAutomationAsync(deviceId, power: true, level: 60, time: string.Empty, weather: "rain");

        var engine = CreateEngine(new ThrowingWeatherService());
        await engine.RunTickAsync(CancellationToken.None);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.False(d.Power);
        Assert.Equal(0, d.Level);
    }

    [Fact]
    public async Task DeviceAlreadyInTargetState_NoUpdate()
    {
        var deviceId = await AddDeviceAsync(power: true, level: 50);
        await AddAutomationAsync(deviceId, power: true, level: 50, time: string.Empty, weather: string.Empty);

        var weather = new FakeWeatherService(false);
        var engine = CreateEngine(weather);
        await engine.RunTickAsync(CancellationToken.None);

        // Should not have called weather since no automation has weather
        Assert.Equal(0, weather.CallCount);

        var d = await ReloadDeviceAsync(deviceId);
        Assert.True(d.Power);
        Assert.Equal(50, d.Level);
    }

    [Fact]
    public async Task WeatherCalled_OnlyOncePerTick()
    {
        var d1 = await AddDeviceAsync(false, 0);
        var d2 = await AddDeviceAsync(false, 0);
        await AddAutomationAsync(d1, true, 10, string.Empty, "rain");
        await AddAutomationAsync(d2, true, 20, string.Empty, "rain");

        var weather = new FakeWeatherService(true);
        var engine = CreateEngine(weather);
        await engine.RunTickAsync(CancellationToken.None);

        Assert.Equal(1, weather.CallCount);
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        private readonly bool _isRaining;
        public int CallCount { get; private set; }

        public FakeWeatherService(bool isRaining) => _isRaining = isRaining;

        public Task<bool> IsRainingAsync()
        {
            CallCount++;
            return Task.FromResult(_isRaining);
        }

        public Task<(double? Latitude, double? Longitude)> GetCoordinatesAsync(string cityName)
            => Task.FromResult<(double?, double?)>((null, null));

        public Task<WeatherInfo> GetCurrentWeatherAsync()
            => Task.FromResult(new WeatherInfo { IsRaining = _isRaining });
    }

    private sealed class ThrowingWeatherService : IWeatherService
    {
        public Task<bool> IsRainingAsync() => throw new InvalidOperationException("boom");
        public Task<(double? Latitude, double? Longitude)> GetCoordinatesAsync(string cityName) => throw new InvalidOperationException();
        public Task<WeatherInfo> GetCurrentWeatherAsync() => throw new InvalidOperationException();
    }

    // Wraps the root provider so the engine's CreateScope() yields a scope with our weather service registered.
    private sealed class ScopedFactoryWithWeather : IServiceScopeFactory
    {
        private readonly IServiceProvider _root;
        private readonly IWeatherService _weather;

        public ScopedFactoryWithWeather(IServiceProvider root, IWeatherService weather)
        {
            _root = root;
            _weather = weather;
        }

        public IServiceScope CreateScope()
        {
            var inner = _root.GetRequiredService<IServiceScopeFactory>().CreateScope();
            return new WeatherInjectingScope(inner, _weather);
        }

        private sealed class WeatherInjectingScope : IServiceScope
        {
            private readonly IServiceScope _inner;
            private readonly InjectingProvider _provider;

            public WeatherInjectingScope(IServiceScope inner, IWeatherService weather)
            {
                _inner = inner;
                _provider = new InjectingProvider(inner.ServiceProvider, weather);
            }

            public IServiceProvider ServiceProvider => _provider;
            public void Dispose() => _inner.Dispose();
        }

        private sealed class InjectingProvider : IServiceProvider
        {
            private readonly IServiceProvider _inner;
            private readonly IWeatherService _weather;

            public InjectingProvider(IServiceProvider inner, IWeatherService weather)
            {
                _inner = inner;
                _weather = weather;
            }

            public object? GetService(Type serviceType)
            {
                if (serviceType == typeof(IWeatherService))
                    return _weather;
                return _inner.GetService(serviceType);
            }
        }
    }
}
