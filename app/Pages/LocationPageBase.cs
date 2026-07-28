using Microsoft.AspNetCore.Components;
using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.App.Pages;

public abstract class LocationPageBase : ComponentBase
{
    [Inject] protected WeatherService Api { get; set; } = default!;
    [Inject] protected LocationState Location { get; set; } = default!;

    protected CityLocation? City { get; private set; }
    protected WeatherForecast? Forecast { get; private set; }
    protected bool Loading { get; private set; }
    protected string? Error { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        await Location.InitializeAsync();
        if (Location.Current is not null) await LoadCityAsync(Location.Current);
    }

    protected async Task LoadCityAsync(CityLocation city)
    {
        City = city;
        Loading = true;
        Error = null;
        await InvokeAsync(StateHasChanged);
        try
        {
            Forecast = await Api.GetForecastAsync(city);
        }
        catch
        {
            Error = "Weather is taking a rain check. Please try that search again.";
        }
        finally
        {
            Loading = false;
        }
    }
}
