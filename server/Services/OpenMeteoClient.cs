using System.Net.Http.Json;
using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.Server.Services;

public sealed class OpenMeteoClient(HttpClient http, StateEnergyRepository states)
{
    public async Task<IReadOnlyList<CityLocation>> SearchCitiesAsync(string query, CancellationToken cancellationToken)
    {
        var url = $"v1/search?name={Uri.EscapeDataString(query)}&count=8&language=en&format=json";
        using var response = await http.GetAsync($"https://geocoding-api.open-meteo.com/{url}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CitySearchResponse>(cancellationToken: cancellationToken))
            ?.Results
            .Where(city => string.Equals(city.CountryCode, "US", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? [];
    }

    public async Task<WeatherForecast> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        const string current = "temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m";
        const string daily = "temperature_2m_max,temperature_2m_min,shortwave_radiation_sum";
        var url = $"v1/forecast?latitude={latitude:R}&longitude={longitude:R}&current={current}&daily={daily}&temperature_unit=fahrenheit&wind_speed_unit=mph&timezone=auto";
        return await http.GetFromJsonAsync<WeatherForecast>(url, cancellationToken)
               ?? throw new InvalidDataException("Open-Meteo returned an empty forecast.");
    }

    public async Task<CorrelationResponse> GetCorrelationAsync(
        double latitude,
        double longitude,
        string stateName,
        CancellationToken cancellationToken)
    {
        var dataset = await states.GetDatasetAsync(cancellationToken);
        if (!dataset.States.TryGetValue(stateName, out var state) || state.History.Count < 4)
        {
            return new CorrelationResponse(null, 0);
        }

        var ordered = state.History.OrderBy(row => row.Period).ToList();
        var start = $"{ordered[0].Period}-01-01";
        var end = $"{ordered[^1].Period}-12-31";
        var url = $"v1/archive?latitude={latitude:R}&longitude={longitude:R}&start_date={start}&end_date={end}&daily=temperature_2m_mean&temperature_unit=fahrenheit&timezone=auto";
        using var response = await http.GetAsync($"https://archive-api.open-meteo.com/{url}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<HistoricalWeather>(cancellationToken: cancellationToken);
        if (history is null)
        {
            return new CorrelationResponse(null, 0);
        }

        var annualTemperatures = history.Daily.Dates
            .Select((date, index) => new { Year = date[..4], Value = history.Daily.MeanTemperatures.ElementAtOrDefault(index) })
            .Where(item => item.Value.HasValue)
            .GroupBy(item => item.Year)
            .ToDictionary(group => group.Key, group => group.Average(item => item.Value!.Value));
        var pairs = ordered
            .Where(row => annualTemperatures.ContainsKey(row.Period))
            .Select(row => (X: annualTemperatures[row.Period], Y: row.AverageMonthlyKwh))
            .ToList();
        return new CorrelationResponse(EnergyCalculations.Pearson(pairs), pairs.Count);
    }
}
