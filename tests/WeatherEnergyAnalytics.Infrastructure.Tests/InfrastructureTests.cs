using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WeatherEnergyAnalytics.Infrastructure.Data;
using WeatherEnergyAnalytics.Infrastructure.Seed;

namespace WeatherEnergyAnalytics.Infrastructure.Tests;

public class InfrastructureTests
{
    [Fact]
    public async Task Seeder_creates_two_years_and_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<WeatherEnergyDbContext>()
            .UseSqlite("Data Source=:memory:").Options;
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        options = new DbContextOptionsBuilder<WeatherEnergyDbContext>().UseSqlite(connection).Options;
        var factory = new TestFactory(options);
        await using (var db = await factory.CreateDbContextAsync()) await db.Database.EnsureCreatedAsync();
        var seeder = new SyntheticDataSeeder(factory);
        (await seeder.SeedAsync()).Should().Be(730);
        (await seeder.SeedAsync()).Should().Be(0);
        await using var verify = await factory.CreateDbContextAsync();
        (await verify.EnergyUsageRecords.CountAsync()).Should().Be(730);
    }

    private sealed class TestFactory(DbContextOptions<WeatherEnergyDbContext> options)
        : IDbContextFactory<WeatherEnergyDbContext>
    {
        public WeatherEnergyDbContext CreateDbContext() => new(options);
        public Task<WeatherEnergyDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
