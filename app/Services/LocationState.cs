using System.Text.Json;
using Microsoft.JSInterop;
using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class LocationState(IJSRuntime js)
{
    private const string StorageKey = "wattweather.location.v3";
    private bool _initialized;

    public CityLocation? Current { get; private set; }
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        try
        {
            Current = JsonSerializer.Deserialize<CityLocation>(stored);
        }
        catch (JsonException)
        {
            await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
    }

    public async Task SelectAsync(CityLocation city)
    {
        Current = city;
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(city));
        Changed?.Invoke();
    }
}
