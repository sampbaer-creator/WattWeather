using WeatherEnergyAnalytics.Web.Components;
using WeatherEnergyAnalytics.Infrastructure.DependencyInjection;
using WeatherEnergyAnalytics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
builder.Services.AddWeatherEnergyInfrastructure(
    $"Data Source={Path.Combine(dataDirectory, "weather-energy.db")}",
    options => options.ApiKey = builder.Configuration["OpenWeather:ApiKey"]
        ?? Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY") ?? string.Empty);

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WeatherEnergyDbContext>>();
    await using var context = await factory.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<WeatherEnergyAnalytics.Core.Contracts.ISampleDataSeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
