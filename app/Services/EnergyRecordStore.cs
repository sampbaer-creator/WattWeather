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
        var parsed = ParseDailyCsv(csv);
        foreach (var record in parsed)
        {
            _records.RemoveAll(existing => existing.Date == record.Date);
            _records.Add(record);
        }
        await SaveAsync();
        return parsed.Count;
    }

    public static IReadOnlyList<EnergyRecord> ParseDailyCsv(string csv)
    {
        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 4)
        {
            throw new InvalidOperationException("Full-history analysis needs at least three daily records. For one monthly bill, use the quick comparison on Overview.");
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

        var parsed = new List<EnergyRecord>();
        var rowNumber = 1;
        foreach (var line in lines.Skip(1))
        {
            rowNumber++;
            var cells = line.Split(',').Select(value => value.Trim().Trim('"')).ToArray();
            if (cells.Length <= Math.Max(dateIndex, usageIndex) ||
                !DateOnly.TryParse(cells[dateIndex], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
                !double.TryParse(cells[usageIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var usage) ||
                !double.IsFinite(usage) ||
                usage is <= 0 or > 1_000_000)
            {
                throw new InvalidOperationException($"CSV row {rowNumber} has an invalid date or kWh value. Fix the row and import again.");
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

            parsed.Add(new EnergyRecord { Date = date, KilowattHours = usage, Cost = cost, MeanTemperature = temperature });
        }

        if (parsed.Select(record => record.Date).Distinct().Count() != parsed.Count)
        {
            throw new InvalidOperationException("The CSV contains duplicate dates. Keep one daily record per date and import again.");
        }
        var dates = parsed.Select(record => record.Date).Order().ToList();
        var gaps = dates.Zip(dates.Skip(1), (left, right) => right.DayNumber - left.DayNumber).Order().ToList();
        var medianGap = gaps[gaps.Count / 2];
        if (medianGap > 3)
        {
            throw new InvalidOperationException("These records look weekly or monthly. Full-history weather matching requires daily rows; use the one-bill comparison on Overview for a monthly bill.");
        }
        return parsed;
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
