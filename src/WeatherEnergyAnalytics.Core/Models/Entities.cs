using System.ComponentModel.DataAnnotations;

namespace WeatherEnergyAnalytics.Core.Models;

public enum WeatherDataSource
{
    LiveOpenWeather,
    HistoricalOpenMeteo,
    SavedObservation,
    Manual,
    Synthetic
}

public enum HeatingType
{
    Electric,
    NaturalGas,
    HeatPump,
    Propane,
    Other,
    None
}

public sealed class Location
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Region { get; set; }

    [Required, MaxLength(2)]
    public string CountryCode { get; set; } = "US";

    [MaxLength(16)]
    public string? PostalCode { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required, MaxLength(160)]
    public string NormalizedKey { get; set; } = string.Empty;

    public bool IsFavorite { get; set; }
    public DateTimeOffset LastSearchedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class HouseholdProfile
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = "Primary household";

    [Range(100, 100_000)]
    public int HomeSizeSquareFeet { get; set; }

    [Range(1, 50)]
    public int OccupantCount { get; set; }

    public HeatingType HeatingType { get; set; }

    [Range(0, 10)]
    public decimal DefaultElectricityRate { get; set; }

    public int? DefaultLocationId { get; set; }
    public Location? DefaultLocation { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class WeatherObservation
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public DateOnly ObservationDate { get; set; }
    public DateTimeOffset? ObservedAtUtc { get; set; }
    public double TemperatureF { get; set; }
    public double FeelsLikeF { get; set; }
    public double HighTemperatureF { get; set; }
    public double LowTemperatureF { get; set; }
    public double HumidityPercent { get; set; }
    public double WindSpeedMph { get; set; }

    [Required, MaxLength(120)]
    public string Condition { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? IconCode { get; set; }

    public DateTimeOffset? Sunrise { get; set; }
    public DateTimeOffset? Sunset { get; set; }
    public WeatherDataSource Source { get; set; }
    public bool IsSynthetic { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class EnergyUsageRecord
{
    public int Id { get; set; }
    public DateOnly UsageDate { get; set; }

    [Range(0.01, 100_000)]
    public double ElectricityUsageKwh { get; set; }

    [Range(0, 100_000)]
    public decimal TotalElectricityCost { get; set; }

    [Range(0, 10)]
    public decimal CostPerKwh { get; set; }

    public int LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public int HouseholdProfileId { get; set; }
    public HouseholdProfile HouseholdProfile { get; set; } = null!;
    public int? WeatherObservationId { get; set; }
    public WeatherObservation? WeatherObservation { get; set; }

    [Range(100, 100_000)]
    public int HomeSizeSquareFeet { get; set; }

    [Range(1, 50)]
    public int OccupantCount { get; set; }

    public HeatingType HeatingType { get; set; }

    [Range(0, 24)]
    public double AirConditioningHours { get; set; }

    [MaxLength(1_000)]
    public string? Notes { get; set; }

    public bool IsSynthetic { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ApplicationSetting
{
    [Key, MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2_000)]
    public string Value { get; set; } = string.Empty;
}

public sealed record WeatherSnapshot(
    string City,
    string? Region,
    string CountryCode,
    string? PostalCode,
    double Latitude,
    double Longitude,
    double TemperatureF,
    double FeelsLikeF,
    double HighTemperatureF,
    double LowTemperatureF,
    double HumidityPercent,
    double WindSpeedMph,
    string Description,
    string? IconCode,
    DateTimeOffset Sunrise,
    DateTimeOffset Sunset,
    DateTimeOffset ObservedAtUtc);

public sealed record EnergyDataPoint(
    DateOnly Date,
    double UsageKwh,
    decimal Cost,
    decimal CostPerKwh,
    double AverageTemperatureF,
    double MinimumTemperatureF,
    double MaximumTemperatureF,
    double HumidityPercent,
    int HomeSizeSquareFeet,
    int OccupantCount,
    HeatingType HeatingType,
    double AirConditioningHours,
    bool IsSynthetic);
