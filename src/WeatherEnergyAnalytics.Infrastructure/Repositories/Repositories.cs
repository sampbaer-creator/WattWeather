using Microsoft.EntityFrameworkCore;
using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;
using WeatherEnergyAnalytics.Infrastructure.Data;

namespace WeatherEnergyAnalytics.Infrastructure.Repositories;

public sealed class EnergyRepository(IDbContextFactory<WeatherEnergyDbContext> factory) : IEnergyRepository
{
    public async Task<IReadOnlyList<EnergyUsageRecord>> GetAsync(
        DateOnly? from = null, DateOnly? to = null, int? locationId = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.EnergyUsageRecords.AsNoTracking()
            .Include(x => x.Location).Include(x => x.HouseholdProfile).Include(x => x.WeatherObservation)
            .AsQueryable();
        if (from is not null) query = query.Where(x => x.UsageDate >= from);
        if (to is not null) query = query.Where(x => x.UsageDate <= to);
        if (locationId is not null) query = query.Where(x => x.LocationId == locationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Location.Name.Contains(term) || (x.Notes != null && x.Notes.Contains(term)));
        }
        return await query.OrderByDescending(x => x.UsageDate).ToListAsync(cancellationToken);
    }

    public async Task<EnergyUsageRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.EnergyUsageRecords.AsNoTracking()
            .Include(x => x.Location).Include(x => x.HouseholdProfile).Include(x => x.WeatherObservation)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<EnergyUsageRecord> UpsertAsync(EnergyUsageRecord record, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (record.Id == 0) db.EnergyUsageRecords.Add(record);
        else db.EnergyUsageRecords.Update(record);
        await db.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.EnergyUsageRecords.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class LocationRepository(IDbContextFactory<WeatherEnergyDbContext> factory) : ILocationRepository
{
    public async Task<IReadOnlyList<Location>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Locations.AsNoTracking().OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.LastSearchedAtUtc).Take(12).ToListAsync(cancellationToken);
    }

    public async Task<Location> UpsertAsync(Location location, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Locations.SingleOrDefaultAsync(x => x.NormalizedKey == location.NormalizedKey, cancellationToken);
        if (existing is null) db.Locations.Add(location);
        else
        {
            existing.Name = location.Name;
            existing.Region = location.Region;
            existing.PostalCode = location.PostalCode;
            existing.Latitude = location.Latitude;
            existing.Longitude = location.Longitude;
            existing.IsFavorite = location.IsFavorite || existing.IsFavorite;
            existing.LastSearchedAtUtc = DateTimeOffset.UtcNow;
            location = existing;
        }
        await db.SaveChangesAsync(cancellationToken);
        return location;
    }
}
