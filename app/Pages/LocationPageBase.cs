using Microsoft.AspNetCore.Components;
using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.App.Pages;

public abstract class LocationPageBase : ComponentBase
{
    [Inject] protected WeatherEnergyService Api { get; set; } = default!;
    [Inject] protected LocationState Location { get; set; } = default!;
    [Inject] protected EnergyCalculations Calculations { get; set; } = default!;

    protected CityLocation? City { get; private set; }
    protected WeatherForecast? Forecast { get; private set; }
    protected StateEnergy? StateEnergy { get; private set; }
    protected bool Loading { get; private set; }
    protected string? Error { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await Location.InitializeAsync();
        if (Location.Current is not null)
        {
            await LoadCityAsync(Location.Current);
        }
    }

    protected async Task LoadCityAsync(CityLocation city)
    {
        City = city;
        Loading = true;
        Error = null;
        await InvokeAsync(StateHasChanged);
        try
        {
            var forecastTask = Api.GetForecastAsync(city);
            var stateTask = Api.GetStateEnergyAsync(city.State);
            await Task.WhenAll(forecastTask, stateTask);
            Forecast = await forecastTask;
            StateEnergy = await stateTask;
            await OnLocationLoadedAsync();
        }
        catch
        {
            Error = "Live city data is temporarily unavailable. Try again in a moment.";
        }
        finally
        {
            Loading = false;
        }
    }

    protected virtual Task OnLocationLoadedAsync() => Task.CompletedTask;
}
