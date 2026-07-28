using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.Tests;

public sealed class EnergyCalculationsTests
{
    private readonly EnergyCalculations _calculations = new();

    [Fact]
    public void CalculateSolar_UsesRadiationEfficiencyAndLocalPrice()
    {
        var forecast = new WeatherForecast
        {
            Daily = new DailyWeather
            {
                SolarRadiationMegajoules = [18, 18, 18]
            }
        };
        var state = new StateEnergy { ResidentialPriceCents = 20 };

        var result = _calculations.CalculateSolar(forecast, state);

        Assert.Equal(5, result.DailySolarKwhPerSquareMeter, 6);
        Assert.Equal(8_760, result.AnnualOutputKwh, 6);
        Assert.Equal(1_752, result.AnnualBillValue, 6);
        Assert.InRange(result.Score, 0, 100);
    }

    [Theory]
    [InlineData(40, 25, 0)]
    [InlineData(65, 0, 0)]
    [InlineData(90, 0, 25)]
    public void CalculateDemand_SplitsHeatingAndCoolingPressure(
        double temperature,
        double expectedHeating,
        double expectedCooling)
    {
        var result = EnergyCalculations.CalculateDemand(temperature);

        Assert.Equal(expectedHeating, result.HeatingPressure);
        Assert.Equal(expectedCooling, result.CoolingPressure);
    }

    [Fact]
    public void CalculateDiscount_ClampsNegativeValuesAndNeverExceedsQuote()
    {
        var result = EnergyCalculations.CalculateDiscount(20_000, -500, 25_000);

        Assert.Equal(20_000, result.ConfirmedDiscounts);
        Assert.Equal(0, result.NetCost);
        Assert.Equal(100, result.PercentReduction);
    }

    [Fact]
    public void Summarize_ComputesUsageCostAndTemperatureCorrelation()
    {
        EnergyRecord[] records =
        [
            new() { KilowattHours = 10, Cost = 2, MeanTemperature = 40 },
            new() { KilowattHours = 20, Cost = 4, MeanTemperature = 50 },
            new() { KilowattHours = 30, Cost = 6, MeanTemperature = 60 }
        ];

        var result = EnergyCalculations.Summarize(records);

        Assert.Equal(3, result.RecordCount);
        Assert.Equal(60, result.TotalKilowattHours);
        Assert.Equal(20, result.AverageKilowattHours);
        Assert.Equal(12, result.TotalCost);
        Assert.Equal(1, result.TemperatureCorrelation);
    }

    [Fact]
    public void Pearson_RequiresAtLeastThreeUsefulPairs()
    {
        var result = EnergyCalculations.Pearson([(1, 2), (2, 3)]);

        Assert.Null(result);
    }
}
