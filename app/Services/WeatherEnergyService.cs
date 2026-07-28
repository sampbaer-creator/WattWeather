using System.Net.Http.Json;
using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class WeatherEnergyService(HttpClient http)
{
    private Task<EiaDataset?>? _eiaRequest;
    private bool IsStaticHosting =>
        http.BaseAddress?.Host.EndsWith(".github.io", StringComparison.OrdinalIgnoreCase) == true ||
        http.BaseAddress?.AbsolutePath.Contains("/WattWeather/", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<IReadOnlyList<CityLocation>> SearchCitiesAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        if (!IsStaticHosting)
        {
            return await http.GetFromJsonAsync<List<CityLocation>>(
                $"api/locations?query={encodedQuery}",
                cancellationToken) ?? [];
        }

        var response = await http.GetFromJsonAsync<CitySearchResponse>(
            $"https://geocoding-api.open-meteo.com/v1/search?name={encodedQuery}&count=8&language=en&format=json",
            cancellationToken);
        return response?.Results
            .Where(city => string.Equals(city.CountryCode, "US", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
    }

    public async Task<WeatherForecast> GetForecastAsync(CityLocation city, CancellationToken cancellationToken = default)
    {
        var url = IsStaticHosting
            ? $"https://api.open-meteo.com/v1/forecast?latitude={city.Latitude:R}&longitude={city.Longitude:R}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min,shortwave_radiation_sum&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto"
            : $"api/weather?latitude={city.Latitude:R}&longitude={city.Longitude:R}";
        return await http.GetFromJsonAsync<WeatherForecast>(url, cancellationToken)
               ?? throw new InvalidOperationException("Weather data was empty.");
    }

    public async Task<StateEnergy?> GetStateEnergyAsync(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        _eiaRequest ??= http.GetFromJsonAsync<EiaDataset>(
            IsStaticHosting ? "data/eia-state-energy.json" : "api/states");
        var dataset = await _eiaRequest;
        return dataset?.States.GetValueOrDefault(state);
    }

    public async Task<double?> GetTemperatureUsageCorrelationAsync(
        CityLocation city,
        StateEnergy state,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city.State) || state.History.Count < 4) return null;
        if (!IsStaticHosting)
        {
            var apiUrl = $"api/correlation?latitude={city.Latitude:R}&longitude={city.Longitude:R}&state={Uri.EscapeDataString(city.State)}";
            return (await http.GetFromJsonAsync<CorrelationResponse>(apiUrl, cancellationToken))?.Value;
        }

        var ordered = state.History.OrderBy(row => row.Period).ToList();
        var archiveUrl =
            $"https://archive-api.open-meteo.com/v1/archive?latitude={city.Latitude:R}&longitude={city.Longitude:R}&start_date={ordered[0].Period}-01-01&end_date={ordered[^1].Period}-12-31&daily=temperature_2m_mean&temperature_unit=fahrenheit&timezone=auto";
        var history = await http.GetFromJsonAsync<HistoricalWeather>(archiveUrl, cancellationToken);
        if (history is null)
        {
            return null;
        }

        var annualTemperatures = history.Daily.Dates
            .Select((date, index) => new
            {
                Year = date[..4],
                Value = history.Daily.MeanTemperatures.ElementAtOrDefault(index)
            })
            .Where(item => item.Value.HasValue)
            .GroupBy(item => item.Year)
            .ToDictionary(group => group.Key, group => group.Average(item => item.Value!.Value));
        var pairs = ordered
            .Where(row => annualTemperatures.ContainsKey(row.Period))
            .Select(row => (X: annualTemperatures[row.Period], Y: row.AverageMonthlyKwh))
            .ToList();
        return EnergyCalculations.Pearson(pairs);
    }
}
