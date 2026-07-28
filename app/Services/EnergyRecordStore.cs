using System.Globalization;
using System.Text.Json;
using Microsoft.JSInterop;
using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class EnergyRecordStore(IJSRuntime js)
{
    private const string StorageKey = "wattweather.energy.v2";
    private readonly List<EnergyRecord> _records = [];
    private bool _loaded;

    public IReadOnlyList<EnergyRecord> Records => _records.OrderByDescending(record => record.Date).ToList();

    public async Task LoadAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(stored))
        {
            return;
        }

        try
        {
            _records.AddRange(JsonSerializer.Deserialize<List<EnergyRecord>>(stored) ?? []);
        }
        catch (JsonException)
        {
            await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
    }

    public async Task AddAsync(EnergyRecord record)
    {
        _records.RemoveAll(existing => existing.Date == record.Date);
        _records.Add(record);
        await SaveAsync();
    }

    public async Task RemoveAsync(Guid id)
    {
        _records.RemoveAll(record => record.Id == id);
        await SaveAsync();
    }

    public async Task ClearAsync()
    {
        _records.Clear();
        await SaveAsync();
    }

    public async Task<int> ImportCsvAsync(string csv)
    {
        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("The CSV needs a header and at least one data row.");
        }
        if (lines.Length > 5_001)
        {
            throw new InvalidOperationException("Import at most 5,000 records at a time.");
        }

        var headers = lines[0].Split(',').Select(value => Normalize(value)).ToList();
        var dateIndex = Find(headers, "date", "usagedate", "billingdate");
        var usageIndex = Find(headers, "kwh", "usage", "energykwh");
        var costIndex = Find(headers, "cost", "amount", "totalcost");
        var temperatureIndex = Find(headers, "temperature", "temp", "meantemperature");
        if (dateIndex < 0 || usageIndex < 0)
        {
            throw new InvalidOperationException("CSV columns must include date and kWh.");
        }

        var imported = 0;
        foreach (var line in lines.Skip(1))
        {
            var cells = line.Split(',').Select(value => value.Trim().Trim('"')).ToArray();
            if (cells.Length <= Math.Max(dateIndex, usageIndex) ||
                !DateOnly.TryParse(cells[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !double.TryParse(cells[usageIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var usage) ||
                !double.IsFinite(usage) ||
                usage is <= 0 or > 1_000_000)
            {
                continue;
            }

            decimal? cost = null;
            if (costIndex >= 0 && costIndex < cells.Length &&
                decimal.TryParse(cells[costIndex], NumberStyles.Currency, CultureInfo.InvariantCulture, out var parsedCost))
            {
                cost = parsedCost is >= 0 and <= 1_000_000_000 ? parsedCost : null;
            }

            double? temperature = null;
            if (temperatureIndex >= 0 && temperatureIndex < cells.Length &&
                double.TryParse(cells[temperatureIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTemperature))
            {
                temperature = double.IsFinite(parsedTemperature) && parsedTemperature is >= -150 and <= 150
                    ? parsedTemperature
                    : null;
            }

            _records.RemoveAll(existing => existing.Date == date);
            _records.Add(new EnergyRecord { Date = date, KilowattHours = usage, Cost = cost, MeanTemperature = temperature });
            imported++;
        }

        await SaveAsync();
        return imported;
    }

    private async Task SaveAsync()
    {
        await js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(_records));
    }

    private static int Find(IReadOnlyList<string> headers, params string[] candidates)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (candidates.Contains(headers[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
