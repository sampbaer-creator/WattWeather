using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;
using WeatherEnergyAnalytics.Core.Validation;

namespace WeatherEnergyAnalytics.Infrastructure.Weather;

public sealed class OpenWeatherOptions
{
    public const string SectionName = "OpenWeather";
    public string ApiKey { get; set; } = string.Empty;
}

public sealed class WeatherServiceException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class OpenWeatherService(HttpClient client, IOptions<OpenWeatherOptions> options) : IWeatherService
{
    public async Task<WeatherSnapshot> GetCurrentAsync(string locationQuery, CancellationToken cancellationToken = default)
    {
        var query = InputValidator.NormalizeLocationQuery(locationQuery);
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
            throw new WeatherServiceException("Live weather is not configured. Add OpenWeather:ApiKey using user secrets or an environment variable.");

        var selector = InputValidator.IsValidUsZip(query)
            ? $"zip={Uri.EscapeDataString(query)},US"
            : $"q={Uri.EscapeDataString(query)}";
        try
        {
            using var response = await client.GetAsync($"weather?{selector}&units=imperial&appid={Uri.EscapeDataString(options.Value.ApiKey)}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new WeatherServiceException("That location could not be found. Check the ZIP code or city name.");
            if (!response.IsSuccessStatusCode)
                throw new WeatherServiceException($"The weather provider returned an error ({(int)response.StatusCode}). Try again shortly.");

            var dto = await response.Content.ReadFromJsonAsync<OpenWeatherResponse>(cancellationToken: cancellationToken)
                ?? throw new WeatherServiceException("The weather provider returned an empty response.");
            var condition = dto.Weather.FirstOrDefault();
            var offset = TimeSpan.FromSeconds(dto.Timezone);
            return new WeatherSnapshot(
                dto.Name, dto.Sys.State, dto.Sys.Country, InputValidator.IsValidUsZip(query) ? query : null,
                dto.Coord.Lat, dto.Coord.Lon, dto.Main.Temp, dto.Main.FeelsLike, dto.Main.TempMax,
                dto.Main.TempMin, dto.Main.Humidity, dto.Wind.Speed,
                condition?.Description ?? "Unknown", condition?.Icon,
                DateTimeOffset.FromUnixTimeSeconds(dto.Sys.Sunrise).ToOffset(offset),
                DateTimeOffset.FromUnixTimeSeconds(dto.Sys.Sunset).ToOffset(offset),
                DateTimeOffset.FromUnixTimeSeconds(dto.Timestamp));
        }
        catch (WeatherServiceException) { throw; }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new WeatherServiceException("The weather request timed out. Check your connection and try again.");
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherServiceException("Weather is temporarily unavailable. Check your internet connection.", ex);
        }
    }

    private sealed record OpenWeatherResponse(
        Coordinates Coord, MainMeasurements Main, WindMeasurements Wind,
        SystemMeasurements Sys, WeatherCondition[] Weather, string Name, int Timezone,
        [property: JsonPropertyName("dt")] long Timestamp);
    private sealed record Coordinates(double Lon, double Lat);
    private sealed record MainMeasurements(
        double Temp,
        [property: JsonPropertyName("feels_like")] double FeelsLike,
        [property: JsonPropertyName("temp_min")] double TempMin,
        [property: JsonPropertyName("temp_max")] double TempMax,
        double Humidity);
    private sealed record WindMeasurements(double Speed);
    private sealed record SystemMeasurements(string Country, long Sunrise, long Sunset, string? State);
    private sealed record WeatherCondition(string Description, string Icon);
}
