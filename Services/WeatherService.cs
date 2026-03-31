using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System;

namespace Progetto_Web_2_IoT_Auth.Services
{
    public interface IWeatherService
    {
        Task<(double? Latitude, double? Longitude)> GetCoordinatesAsync(string cityName);
        Task<bool> IsRainingAsync();
 
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
            var cityName = await _settingsService.GetSettingAsync("WeatherCity", "Roma");

            try
            {
                // 1. Geocoding: Get coordinates from the city name
                var (latitude, longitude) = await GetCoordinatesAsync(cityName);

                if (!latitude.HasValue || !longitude.HasValue) return false;

                // 2. Weather: Get weather using coordinates
                var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.Value}&longitude={longitude.Value}&current_weather=true";
                var weatherResponse = await _httpClient.GetAsync(weatherUrl);

                if (weatherResponse.IsSuccessStatusCode)
                {
                    var weatherJson = await weatherResponse.Content.ReadFromJsonAsync<JsonObject>();
                    
                    if (weatherJson != null && weatherJson["current_weather"] != null)
                    {
                        var weatherCode = weatherJson["current_weather"]?["weathercode"]?.GetValue<int>();

                        if (weatherCode.HasValue)
                        {
                            // Open-Meteo codes for rain 51-99
                            return weatherCode.Value is >= 51 and <= 99;
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Error retrieving weather for {cityName}: {ex.Message}");
            }

            return false;
        }
    }
}
