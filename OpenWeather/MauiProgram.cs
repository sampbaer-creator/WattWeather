using Microsoft.Extensions.Logging;
using OpenWeather.ViewModels;
using WeatherEnergyAnalytics.Infrastructure.DependencyInjection;
namespace OpenWeather;
public static class MauiProgram
{
 public static MauiApp CreateMauiApp()
 {
  var builder=MauiApp.CreateBuilder();
  builder.UseMauiApp<App>().ConfigureFonts(f=>{f.AddFont("OpenSans-Regular.ttf","OpenSansRegular");f.AddFont("OpenSans-Semibold.ttf","OpenSansSemibold");});
  var databasePath=Path.Combine(FileSystem.AppDataDirectory,"weather-energy.db");
  builder.Services.AddWeatherEnergyInfrastructure($"Data Source={databasePath}",o=>o.ApiKey=Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY")??string.Empty);
  builder.Services.AddTransient<WeatherViewModel>();builder.Services.AddTransient<MainPage>();
#if DEBUG
  builder.Logging.AddDebug();
#endif
  return builder.Build();
 }
}
