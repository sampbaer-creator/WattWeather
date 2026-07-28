using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WattWeather.App;
using WattWeather.App.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<WeatherEnergyService>();
builder.Services.AddScoped<LocationState>();
builder.Services.AddScoped<EnergyRecordStore>();
builder.Services.AddSingleton<EnergyCalculations>();

await builder.Build().RunAsync();
