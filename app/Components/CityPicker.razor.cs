using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.App.Components;

public partial class CityPicker : IAsyncDisposable
{
    [Inject] private WeatherEnergyService Api { get; set; } = default!;
    [Inject] private LocationState Location { get; set; } = default!;
    [Parameter] public EventCallback<CityLocation> Selected { get; set; }
    [Parameter] public string ButtonText { get; set; } = "Use this city";

    protected string InputId { get; } = $"city-{Guid.NewGuid():N}";
    protected string Query { get; set; } = "";
    protected List<CityLocation> Results { get; set; } = [];
    protected string Status { get; set; } = "";
    protected bool HasError { get; set; }
    protected bool IsBusy { get; set; }
    private CancellationTokenSource? _searchCancellation;

    protected override async Task OnInitializedAsync()
    {
        await Location.InitializeAsync();
        Query = Location.Current?.Label ?? "";
    }

    protected async Task HandleInput(ChangeEventArgs args)
    {
        Query = args.Value?.ToString() ?? "";
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        if (Query.Trim().Length < 2)
        {
            Results.Clear();
            Status = "";
            return;
        }

        try
        {
            await Task.Delay(350, _searchCancellation.Token);
            IsBusy = true;
            Results = (await Api.SearchCitiesAsync(Query, _searchCancellation.Token)).ToList();
            Status = Results.Count == 0 ? "No matching cities found." : "";
            HasError = false;
        }
        catch (OperationCanceledException) { }
        catch
        {
            Results.Clear();
            Status = "City search is temporarily unavailable.";
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await UseFirstResultAsync();
        }
    }

    protected async Task UseFirstResultAsync()
    {
        if (Results.Count == 0 && Query.Trim().Length >= 2)
        {
            IsBusy = true;
            try
            {
                Results = (await Api.SearchCitiesAsync(Query)).ToList();
            }
            catch
            {
                Status = "City search is temporarily unavailable.";
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        if (Results.Count > 0)
        {
            await SelectAsync(Results[0]);
        }
        else
        {
            Status = "Choose a city from the search results.";
            HasError = true;
        }
    }

    protected async Task SelectAsync(CityLocation city)
    {
        Query = city.Label;
        Results.Clear();
        Status = $"Using {city.Label}";
        HasError = false;
        await Location.SelectAsync(city);
        await Selected.InvokeAsync(city);
    }

    public ValueTask DisposeAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
