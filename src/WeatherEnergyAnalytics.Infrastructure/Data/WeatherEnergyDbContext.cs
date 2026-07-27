using Microsoft.EntityFrameworkCore;
using WeatherEnergyAnalytics.Core.Models;

namespace WeatherEnergyAnalytics.Infrastructure.Data;

public sealed class WeatherEnergyDbContext(DbContextOptions<WeatherEnergyDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<HouseholdProfile> HouseholdProfiles => Set<HouseholdProfile>();
    public DbSet<WeatherObservation> WeatherObservations => Set<WeatherObservation>();
    public DbSet<EnergyUsageRecord> EnergyUsageRecords => Set<EnergyUsageRecord>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Location>().HasIndex(x => x.NormalizedKey).IsUnique();
        modelBuilder.Entity<HouseholdProfile>().Property(x => x.DefaultElectricityRate).HasPrecision(10, 4);
        modelBuilder.Entity<EnergyUsageRecord>().Property(x => x.TotalElectricityCost).HasPrecision(12, 2);
        modelBuilder.Entity<EnergyUsageRecord>().Property(x => x.CostPerKwh).HasPrecision(10, 4);
        modelBuilder.Entity<WeatherObservation>()
            .HasIndex(x => new { x.LocationId, x.ObservationDate, x.Source })
            .IsUnique();
        modelBuilder.Entity<EnergyUsageRecord>()
            .HasIndex(x => new { x.HouseholdProfileId, x.LocationId, x.UsageDate, x.IsSynthetic })
            .IsUnique();
        modelBuilder.Entity<EnergyUsageRecord>()
            .HasOne(x => x.WeatherObservation)
            .WithMany()
            .HasForeignKey(x => x.WeatherObservationId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<HouseholdProfile>()
            .HasOne(x => x.DefaultLocation)
            .WithMany()
            .HasForeignKey(x => x.DefaultLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
