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
    [JsonPropertyName("results")]
    public List<CityLocation> Results { get; init; } = [];
}

public sealed class WeatherForecast
{
    [JsonPropertyName("current")]
    public CurrentWeather Current { get; init; } = new();

    [JsonPropertyName("daily")]
    public DailyWeather Daily { get; init; } = new();
}

public sealed class CurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; init; }

    [JsonPropertyName("apparent_temperature")]
    public double FeelsLike { get; init; }

    [JsonPropertyName("relative_humidity_2m")]
    public double Humidity { get; init; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; init; }
}

public sealed class DailyWeather
{
    [JsonPropertyName("time")]
    public List<string> Dates { get; init; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double> Highs { get; init; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double> Lows { get; init; } = [];

    [JsonPropertyName("shortwave_radiation_sum")]
    public List<double> SolarRadiationMegajoules { get; init; } = [];
}

public sealed class HistoricalWeather
{
    [JsonPropertyName("daily")]
    public HistoricalDaily Daily { get; init; } = new();
}

public sealed class HistoricalDaily
{
    [JsonPropertyName("time")]
    public List<string> Dates { get; init; } = [];

    [JsonPropertyName("temperature_2m_mean")]
    public List<double?> MeanTemperatures { get; init; } = [];
}

public sealed class EiaDataset
{
    [JsonPropertyName("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; init; }

    [JsonPropertyName("states")]
    public Dictionary<string, StateEnergy> States { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class StateEnergy
{
    [JsonPropertyName("period")]
    public string Period { get; init; } = "";

    [JsonPropertyName("residentialPriceCents")]
    public double ResidentialPriceCents { get; init; }

    [JsonPropertyName("averageMonthlyKwh")]
    public double AverageMonthlyKwh { get; init; }

    [JsonPropertyName("history")]
    public List<StateEnergyHistory> History { get; init; } = [];
}

public sealed class StateEnergyHistory
{
    [JsonPropertyName("period")]
    public string Period { get; init; } = "";

    [JsonPropertyName("residentialPriceCents")]
    public double ResidentialPriceCents { get; init; }

    [JsonPropertyName("averageMonthlyKwh")]
    public double AverageMonthlyKwh { get; init; }
}

public sealed record SolarEstimate(
    double DailySolarKwhPerSquareMeter,
    double AnnualOutputKwh,
    double AnnualBillValue,
    int Score,
    string Verdict,
    string Explanation);

public sealed record DemandEstimate(
    double HeatingPressure,
    double CoolingPressure,
    string Summary);

public sealed record PowerProfile(
    string Name,
    string Category,
    string Symbol,
    string Description);

public sealed record DiscountEstimate(
    decimal Quote,
    decimal ConfirmedDiscounts,
    decimal NetCost,
    decimal PercentReduction);

public sealed class EnergyRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public double KilowattHours { get; set; }
    public decimal? Cost { get; set; }
    public double? MeanTemperature { get; set; }
}

public sealed record EnergySummary(
    int RecordCount,
    double TotalKilowattHours,
    double AverageKilowattHours,
    decimal TotalCost,
    double? TemperatureCorrelation,
    int UnusualRecordCount);

public sealed record CorrelationResponse(double? Value, int MatchedYears);

public enum BillComparisonBand
{
    Below,
    Near,
    Above
}

public sealed record BillComparison(
    double KilowattHours,
    bool IsEstimated,
    double DifferencePercent,
    BillComparisonBand Band,
    string Headline,
    string Guidance);

public sealed record DegreeDaySummary(double HeatingDegreeDays, double CoolingDegreeDays);
