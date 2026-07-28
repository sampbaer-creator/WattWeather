using WattWeather.App.Models;
using WattWeather.App.Services;

namespace WattWeather.Tests;

public sealed class EnergyCalculationsTests
{
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

    [Fact]
    public void Pearson_ReturnsNullWhenTemperatureHasNoVariance()
    {
        var result = EnergyCalculations.Pearson([(32, 10), (32, 20), (32, 30), (32, 1_000)]);

        Assert.Null(result);
    }

    [Fact]
    public void Pearson_RemainsFiniteWithHistoricFreezeOutlier()
    {
        var result = EnergyCalculations.Pearson(
            [(-40, 140), (25, 90), (45, 65), (65, 45), (85, 80), (105, 130)]);

        Assert.NotNull(result);
        Assert.True(double.IsFinite(result.Value));
        Assert.InRange(result.Value, -1, 1);
    }

    [Fact]
    public void Summarize_IqrFlagsOneExtremeUsageDayWithoutOverflow()
    {
        EnergyRecord[] records =
        [
            new() { KilowattHours = 10 },
            new() { KilowattHours = 11 },
            new() { KilowattHours = 12 },
            new() { KilowattHours = 13 },
            new() { KilowattHours = 1_000_000 }
        ];

        var result = EnergyCalculations.Summarize(records);

        Assert.Equal(1, result.UnusualRecordCount);
        Assert.True(double.IsFinite(result.TotalKilowattHours));
        Assert.True(double.IsFinite(result.AverageKilowattHours));
    }

    [Fact]
    public void Summarize_IqrDoesNotFlagConstantUsage()
    {
        var records = Enumerable.Range(0, 7)
            .Select(index => new EnergyRecord
            {
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-index)),
                KilowattHours = 25
            })
            .ToArray();

        Assert.Equal(0, EnergyCalculations.Summarize(records).UnusualRecordCount);
    }

    [Theory]
    [InlineData(850, BillComparisonBand.Above, 26.3)]
    [InlineData(673, BillComparisonBand.Near, 0)]
    [InlineData(500, BillComparisonBand.Below, -25.7)]
    public void CompareBill_UsesValidatedOutcomeBands(double usage, BillComparisonBand band, double expectedDifference)
    {
        var state = new StateEnergy { AverageMonthlyKwh = 673, ResidentialPriceCents = 15 };
        var result = EnergyCalculations.CompareBill(100, usage, state);
        Assert.Equal(band, result.Band);
        Assert.Equal(expectedDifference, result.DifferencePercent, 1);
        Assert.False(result.IsEstimated);
    }

    [Fact]
    public void CompareBill_EstimatesUsageWhenOnlyCostIsProvided()
    {
        var state = new StateEnergy { AverageMonthlyKwh = 673, ResidentialPriceCents = 20 };
        var result = EnergyCalculations.CompareBill(100, null, state);
        Assert.Equal(500, result.KilowattHours, 6);
        Assert.True(result.IsEstimated);
    }

    [Fact]
    public void CalculateDegreeDays_UsesSixtyFiveDegreeBaseline()
    {
        var result = EnergyCalculations.CalculateDegreeDays([50, 65, 80]);
        Assert.Equal(15, result.HeatingDegreeDays);
        Assert.Equal(15, result.CoolingDegreeDays);
    }

    [Fact]
    public void ParseDailyCsv_RejectsMonthlyCadence()
    {
        var csv = "date,kwh\n2026-01-01,20\n2026-02-01,21\n2026-03-01,22";
        var error = Assert.Throws<InvalidOperationException>(() => EnergyRecordStore.ParseDailyCsv(csv));
        Assert.Contains("requires daily rows", error.Message);
    }

    [Fact]
    public void ParseDailyCsv_AcceptsDailyCadence()
    {
        var csv = "date,kwh\n2026-01-01,20\n2026-01-02,21\n2026-01-03,22";
        Assert.Equal(3, EnergyRecordStore.ParseDailyCsv(csv).Count);
    }

    [Fact]
    public void PublicShareQuery_ContainsOnlyCityAndState()
    {
        var city = new CityLocation("Denver", 39.73915, -104.9847, "United States", "US", "Colorado");
        var query = EnergyCalculations.BuildPublicShareQuery(city);
        Assert.Equal("?city=Denver&state=Colorado", query);
        Assert.DoesNotContain("kwh", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cost", query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localStorage", query, StringComparison.OrdinalIgnoreCase);
    }
}
