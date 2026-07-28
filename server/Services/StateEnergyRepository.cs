using System.Text.Json;
using WattWeather.App.Models;

namespace WattWeather.Server.Services;

public sealed class StateEnergyRepository
{
    private readonly string _dataPath;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private EiaDataset? _dataset;

    public StateEnergyRepository(IHostEnvironment environment)
    {
        _dataPath = Path.Combine(environment.ContentRootPath, "Data", "eia-state-energy.json");
    }

    public async Task<EiaDataset> GetDatasetAsync(CancellationToken cancellationToken)
    {
        if (_dataset is not null)
        {
            return _dataset;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_dataset is not null)
            {
                return _dataset;
            }

            await using var stream = File.OpenRead(_dataPath);
            _dataset = await JsonSerializer.DeserializeAsync<EiaDataset>(stream, cancellationToken: cancellationToken)
                       ?? throw new InvalidDataException("The EIA dataset is empty.");
            if (_dataset.States.Count < 50)
            {
                throw new InvalidDataException("The EIA dataset does not contain expected state coverage.");
            }

            return _dataset;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
