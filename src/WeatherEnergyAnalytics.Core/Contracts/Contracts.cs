using WeatherEnergyAnalytics.Core.Models;
using WeatherEnergyAnalytics.Core.Analytics;
using WeatherEnergyAnalytics.Core.Forecasting;

namespace WeatherEnergyAnalytics.Core.Contracts;

public interface IWeatherService
{
    Task<WeatherSnapshot> GetCurrentAsync(string locationQuery, CancellationToken cancellationToken = default);
}

public interface IEnergyRepository
{
    Task<IReadOnlyList<EnergyUsageRecord>> GetAsync(
        DateOnly? from = null,
        DateOnly? to = null,
        int? locationId = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<EnergyUsageRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EnergyUsageRecord> UpsertAsync(EnergyUsageRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetRecentAsync(CancellationToken cancellationToken = default);
    Task<Location> UpsertAsync(Location location, CancellationToken cancellationToken = default);
}

public interface ISampleDataSeeder
{
    Task<int> SeedAsync(bool resetExistingSyntheticData = false, CancellationToken cancellationToken = default);
}

public interface IAnalyticsService
{
    AnalyticsSummary Calculate(IReadOnlyCollection<EnergyDataPoint> data);
}

public interface IEnergyForecastService
{
    ForecastModelResult Train(IReadOnlyList<EnergyDataPoint> data);
}
