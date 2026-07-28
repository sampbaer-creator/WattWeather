using System.Text.Json.Serialization;

namespace WattWeather.App.Models;

public sealed record CityLocation(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude,
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("country_code")] string? CountryCode,
    [property: JsonPropertyName("admin1")] string? State)
{
    public string Label => string.Join(", ", new[] { Name, State, Country }.Where(value => !string.IsNullOrWhiteSpace(value)));
}

public sealed class CitySearchResponse
{
    [JsonPropertyName("results")] public List<CityLocation> Results { get; init; } = [];
}

public sealed class WeatherForecast
{
    [JsonPropertyName("timezone_abbreviation")] public string TimezoneAbbreviation { get; init; } = "";
    [JsonPropertyName("current")] public CurrentWeather Current { get; init; } = new();
    [JsonPropertyName("daily")] public DailyWeather Daily { get; init; } = new();
}

public sealed class CurrentWeather
{
    [JsonPropertyName("temperature_2m")] public double Temperature { get; init; }
    [JsonPropertyName("apparent_temperature")] public double FeelsLike { get; init; }
    [JsonPropertyName("relative_humidity_2m")] public double Humidity { get; init; }
    [JsonPropertyName("precipitation")] public double Precipitation { get; init; }
    [JsonPropertyName("cloud_cover")] public double CloudCover { get; init; }
    [JsonPropertyName("surface_pressure")] public double SurfacePressure { get; init; }
    [JsonPropertyName("weather_code")] public int WeatherCode { get; init; }
    [JsonPropertyName("wind_speed_10m")] public double WindSpeed { get; init; }
    [JsonPropertyName("wind_direction_10m")] public double WindDirection { get; init; }
    [JsonPropertyName("is_day")] public int IsDay { get; init; }
}

public sealed class DailyWeather
{
    [JsonPropertyName("time")] public List<string> Dates { get; init; } = [];
    [JsonPropertyName("weather_code")] public List<int> WeatherCodes { get; init; } = [];
    [JsonPropertyName("temperature_2m_max")] public List<double> Highs { get; init; } = [];
    [JsonPropertyName("temperature_2m_min")] public List<double> Lows { get; init; } = [];
    [JsonPropertyName("sunrise")] public List<string> Sunrises { get; init; } = [];
    [JsonPropertyName("sunset")] public List<string> Sunsets { get; init; } = [];
    [JsonPropertyName("precipitation_probability_max")] public List<double> PrecipitationChance { get; init; } = [];
    [JsonPropertyName("precipitation_sum")] public List<double> PrecipitationTotal { get; init; } = [];
    [JsonPropertyName("wind_speed_10m_max")] public List<double> MaxWindSpeed { get; init; } = [];
    [JsonPropertyName("uv_index_max")] public List<double> UvIndex { get; init; } = [];
}
