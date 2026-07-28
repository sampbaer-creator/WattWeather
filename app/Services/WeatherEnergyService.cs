using System.Net.Http.Json;
using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class WeatherEnergyService(HttpClient http)
{
    private Task<EiaDataset?>? _eiaRequest;

    public async Task<IReadOnlyList<CityLocation>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var url = $"api/locations?query={Uri.EscapeDataString(query.Trim())}";
        return await http.GetFromJsonAsync<List<CityLocation>>(url, cancellationToken) ?? [];
    }

    public async Task<WeatherForecast> GetForecastAsync(CityLocation city, CancellationToken cancellationToken = default)
    {
        var url = $"api/weather?latitude={city.Latitude:R}&longitude={city.Longitude:R}";
        return await http.GetFromJsonAsync<WeatherForecast>(url, cancellationToken)
               ?? throw new InvalidOperationException("Weather data was empty.");
    }

    public async Task<StateEnergy?> GetStateEnergyAsync(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        _eiaRequest ??= http.GetFromJsonAsync<EiaDataset>("api/states");
        var dataset = await _eiaRequest;
        return dataset?.States.GetValueOrDefault(state);
    }

    public async Task<double?> GetTemperatureUsageCorrelationAsync(
        CityLocation city,
        StateEnergy state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city.State) || state.History.Count < 4) return null;
        var url = $"api/correlation?latitude={city.Latitude:R}&longitude={city.Longitude:R}&state={Uri.EscapeDataString(city.State)}";
        return (await http.GetFromJsonAsync<CorrelationResponse>(url, cancellationToken))?.Value;
    }
}
