using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WeatherEnergyAnalytics.Core.Analytics;
using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Forecasting;
using WeatherEnergyAnalytics.Infrastructure.Data;
using WeatherEnergyAnalytics.Infrastructure.Repositories;
using WeatherEnergyAnalytics.Infrastructure.Seed;
using WeatherEnergyAnalytics.Infrastructure.Weather;

namespace WeatherEnergyAnalytics.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherEnergyInfrastructure(
        this IServiceCollection services, string connectionString, Action<OpenWeatherOptions>? configureWeather = null)
    {
        services.AddDbContextFactory<WeatherEnergyDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IEnergyRepository, EnergyRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ISampleDataSeeder, SyntheticDataSeeder>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IEnergyForecastService, LinearRegressionForecastService>();
        services.AddOptions<OpenWeatherOptions>();
        if (configureWeather is not null) services.Configure(configureWeather);
        services.AddHttpClient<IWeatherService, OpenWeatherService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
            client.Timeout = TimeSpan.FromSeconds(12);
        });
        return services;
    }
}
