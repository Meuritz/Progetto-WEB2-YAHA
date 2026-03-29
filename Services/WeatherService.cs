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
        Task<bool> IsRainingAsync(double latitude, double longitude);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsRainingAsync(double latitude, double longitude)
        {
            var request_url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current_weather=true";

            try
            {
                var response = await _httpClient.GetAsync(request_url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadFromJsonAsync<JsonObject>();
                    
                    if (jsonResponse != null && jsonResponse["current_weather"] != null)
                    {
                        var weatherCode = jsonResponse["current_weather"]?["weathercode"]?.GetValue<int>();

                        if (weatherCode.HasValue)
                        {
                            // Rain: 51-99
                            return weatherCode.Value is (>= 51 and <= 99);
                        }
                    }
                }
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Error while trying to fetch weather data: {ex.Message}");
            }

            // Ritorniamo false in caso di errore di chiamata, JSON invalido o se non piove
            return false;
        }
    }
}
