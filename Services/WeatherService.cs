using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Progetto_Web_2_IoT_Auth.Services
{
    public class WeatherInfo
    {
        public string City { get; set; } = "";
        public double? Temperature { get; set; }
        public int WeatherCode { get; set; }
        public bool IsRaining { get; set; }
        public string Description { get; set; } = "";
    }

    public interface IWeatherService
    {
        Task<(double? Latitude, double? Longitude)> GetCoordinatesAsync(string cityName);
        Task<bool> IsRainingAsync();
        Task<WeatherInfo> GetCurrentWeatherAsync();
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly SettingsService _settingsService;

        public WeatherService(HttpClient httpClient, SettingsService settingsService)
        {
            _httpClient = httpClient;
            _settingsService = settingsService;
        }

        public async Task<(double? Latitude, double? Longitude)> GetCoordinatesAsync(string cityName)
        {
            try
            {
                var geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cityName)}&count=1";
                var geoResponse = await _httpClient.GetAsync(geoUrl);

                if (!geoResponse.IsSuccessStatusCode) return (null, null);

                var geoJson = await geoResponse.Content.ReadFromJsonAsync<JsonObject>();

                var results = geoJson?["results"]?.AsArray();
                if (results == null || results.Count == 0)
                {
                    Console.WriteLine($"City '{cityName}' not found.");
                    return (null, null);
                }

                var latitude = results[0]?["latitude"]?.GetValue<double>();
                var longitude = results[0]?["longitude"]?.GetValue<double>();

                return (latitude, longitude);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving coordinates for {cityName}: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<bool> IsRainingAsync()
        {
            var weather = await GetCurrentWeatherAsync();
            return weather.IsRaining;
        }

        public async Task<WeatherInfo> GetCurrentWeatherAsync()
        {
            var city = await _settingsService.GetSettingAsync("WeatherCity", "Roma");
            var info = new WeatherInfo { City = city };

            var (lat, lon) = await GetCoordinatesAsync(city);
            if (!lat.HasValue || !lon.HasValue)
            {
                info.Description = "Location not found";
                return info;
            }

            var url = string.Create(CultureInfo.InvariantCulture, $"https://api.open-meteo.com/v1/forecast?latitude={lat.Value}&longitude={lon.Value}&current=temperature_2m,weather_code");
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                info.Description = "Weather unavailable";
                return info;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonObject>();
            var current = json?["current"];

            if (current is null)
            {
                info.Description = "Weather unavailable";
                return info;
            }

            info.Temperature = current["temperature_2m"]?.GetValue<double>();
            info.WeatherCode = current["weather_code"]?.GetValue<int>() ?? 0;
            info.IsRaining = info.WeatherCode is >= 51 and <= 99;

            info.Description = info.WeatherCode switch
            {
                0 => "Clear sky",
                1 or 2 => "Partly cloudy",
                3 => "Overcast",
                >= 45 and <= 48 => "Fog",
                >= 51 and <= 55 => "Drizzle",
                >= 56 and <= 57 => "Freezing drizzle",
                >= 61 and <= 65 => "Rain",
                >= 66 and <= 67 => "Freezing rain",
                >= 71 and <= 77 => "Snow",
                >= 80 and <= 82 => "Rain showers",
                >= 85 and <= 86 => "Snow showers",
                >= 95 and <= 99 => "Thunderstorm",
                _ => "Variable conditions"
            };

            return info;
        }
    }
}
