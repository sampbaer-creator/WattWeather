using System.Net.Http.Json;
using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.Server.Services;

public sealed class OpenMeteoClient(HttpClient http)
{
    public async Task<IReadOnlyList<CityLocation>> SearchCitiesAsync(string query, CancellationToken cancellationToken)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query)}&count=8&language=en&format=json";
        using var response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CitySearchResponse>(cancellationToken: cancellationToken))
            ?.Results.Where(city => string.Equals(city.CountryCode, "US", StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
    }

    public async Task<WeatherForecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        return await http.GetFromJsonAsync<WeatherForecast>(WeatherService.BuildOpenMeteoUrl(latitude, longitude), cancellationToken)
               ?? throw new InvalidDataException("Open-Meteo returned an empty forecast.");
    }
}
