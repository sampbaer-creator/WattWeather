using System.Net.Http.Json;
using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class WeatherService(HttpClient http)
{
    private bool IsStaticHosting =>
        http.BaseAddress?.Host.EndsWith(".github.io", StringComparison.OrdinalIgnoreCase) == true ||
        http.BaseAddress?.AbsolutePath.Contains("/WattWeather/", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<IReadOnlyList<CityLocation>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return [];
        var encoded = Uri.EscapeDataString(query.Trim());
        if (!IsStaticHosting)
        {
            return await http.GetFromJsonAsync<List<CityLocation>>($"api/locations?query={encoded}", cancellationToken) ?? [];
        }
        var response = await http.GetFromJsonAsync<CitySearchResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={encoded}&count=8&language=en&format=json",
            cancellationToken);
        return response?.Results.Where(city =>
            string.Equals(city.CountryCode, "US", StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
    }

    public async Task<WeatherForecast> GetForecastAsync(CityLocation city, CancellationToken cancellationToken = default)
    {
        var url = IsStaticHosting
            ? BuildOpenMeteoUrl(city.Latitude, city.Longitude)
            : $"api/weather?latitude={city.Latitude:R}&longitude={city.Longitude:R}";
        return await http.GetFromJsonAsync<WeatherForecast>(url, cancellationToken)
               ?? throw new InvalidOperationException("Weather data was empty.");
    }

    public static string BuildOpenMeteoUrl(double latitude, double longitude)
    {
        const string current = "temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,cloud_cover,surface_pressure,wind_speed_10m,wind_direction_10m,is_day";
        const string daily = "weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,precipitation_probability_max,precipitation_sum,wind_speed_10m_max,uv_index_max";
        return $"https://api.open-meteo.com/v1/forecast?latitude={latitude:R}&longitude={longitude:R}&current={current}&daily={daily}&temperature_unit=fahrenheit&wind_speed_unit=mph&precipitation_unit=inch&timezone=auto&forecast_days=7";
    }
}
