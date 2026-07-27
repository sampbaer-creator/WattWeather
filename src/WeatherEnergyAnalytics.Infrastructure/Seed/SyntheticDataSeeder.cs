using Microsoft.EntityFrameworkCore;
using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;
using WeatherEnergyAnalytics.Infrastructure.Data;

namespace WeatherEnergyAnalytics.Infrastructure.Seed;

public sealed class SyntheticDataSeeder(IDbContextFactory<WeatherEnergyDbContext> factory) : ISampleDataSeeder
{
    public async Task<int> SeedAsync(bool resetExistingSyntheticData = false, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (resetExistingSyntheticData)
        {
            await db.EnergyUsageRecords.Where(x => x.IsSynthetic).ExecuteDeleteAsync(cancellationToken);
            await db.WeatherObservations.Where(x => x.IsSynthetic).ExecuteDeleteAsync(cancellationToken);
        }
        if (await db.EnergyUsageRecords.AnyAsync(x => x.IsSynthetic, cancellationToken)) return 0;

        var location = await db.Locations.SingleOrDefaultAsync(x => x.NormalizedKey == "denver-co-us", cancellationToken)
            ?? new Location { Name = "Denver", Region = "CO", CountryCode = "US", PostalCode = "80202", Latitude = 39.7392, Longitude = -104.9903, NormalizedKey = "denver-co-us", IsFavorite = true };
        if (location.Id == 0) db.Locations.Add(location);
        var household = await db.HouseholdProfiles.FirstOrDefaultAsync(x => x.IsActive, cancellationToken)
            ?? new HouseholdProfile { Name = "Demo household", HomeSizeSquareFeet = 1850, OccupantCount = 3, HeatingType = HeatingType.HeatPump, DefaultElectricityRate = 0.1425m, DefaultLocation = location };
        if (household.Id == 0) db.HouseholdProfiles.Add(household);
        await db.SaveChangesAsync(cancellationToken);

        const int days = 730;
        var random = new Random(20260727);
        var start = DateOnly.FromDateTime(DateTime.Today).AddDays(-(days - 1));
        var weather = new List<WeatherObservation>(days);
        var energy = new List<EnergyUsageRecord>(days);
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            var seasonal = 54 + 28 * Math.Sin(2 * Math.PI * (date.DayOfYear - 105) / 365.25);
            var average = seasonal + NextGaussian(random, 0, 6);
            var spread = 12 + random.NextDouble() * 8;
            var humidity = Math.Clamp(48 + NextGaussian(random, 0, 12), 15, 92);
            var observation = new WeatherObservation
            {
                LocationId = location.Id, ObservationDate = date, TemperatureF = average,
                FeelsLikeF = average - (humidity > 70 ? 2 : 0), HighTemperatureF = average + spread / 2,
                LowTemperatureF = average - spread / 2, HumidityPercent = humidity,
                WindSpeedMph = Math.Max(0, NextGaussian(random, 9, 4)),
                Condition = humidity > 72 ? "Cloudy" : average < 32 ? "Snow possible" : "Partly cloudy",
                Source = WeatherDataSource.Synthetic, IsSynthetic = true
            };
            weather.Add(observation);
            var heating = Math.Max(0, 65 - average) * 0.38;
            var cooling = Math.Max(0, average - 68) * 0.52;
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 2.2 : 0;
            var usage = 16 + heating + cooling + weekend + NextGaussian(random, 0, 2.1);
            var anomaly = i % 97 == 0 ? 13 + random.NextDouble() * 12 : 0;
            usage = Math.Max(4, usage + anomaly);
            var rate = 0.135m + (decimal)((date.Year - start.Year) * 0.006);
            energy.Add(new EnergyUsageRecord
            {
                UsageDate = date, ElectricityUsageKwh = Math.Round(usage, 2),
                TotalElectricityCost = Math.Round((decimal)usage * rate, 2), CostPerKwh = rate,
                LocationId = location.Id, HouseholdProfileId = household.Id,
                WeatherObservation = observation, HomeSizeSquareFeet = household.HomeSizeSquareFeet,
                OccupantCount = household.OccupantCount, HeatingType = household.HeatingType,
                AirConditioningHours = cooling > 0 ? Math.Min(18, cooling / 2) : 0,
                Notes = anomaly > 0 ? "Synthetic unusual-usage day" : null, IsSynthetic = true
            });
        }
        db.WeatherObservations.AddRange(weather);
        db.EnergyUsageRecords.AddRange(energy);
        await db.SaveChangesAsync(cancellationToken);
        return energy.Count;
    }

    private static double NextGaussian(Random random, double mean, double standardDeviation)
    {
        var u1 = 1 - random.NextDouble();
        var u2 = 1 - random.NextDouble();
        return mean + standardDeviation * Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2);
    }
}
