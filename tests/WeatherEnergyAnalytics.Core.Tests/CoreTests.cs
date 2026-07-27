using FluentAssertions;
using WeatherEnergyAnalytics.Core.Analytics;
using WeatherEnergyAnalytics.Core.Forecasting;
using WeatherEnergyAnalytics.Core.Models;
using WeatherEnergyAnalytics.Core.Validation;

namespace WeatherEnergyAnalytics.Core.Tests;

public class CoreTests
{
    [Theory]
    [InlineData("80202", true)]
    [InlineData("Denver, CO", true)]
    [InlineData("", false)]
    [InlineData("<script>", false)]
    public void Location_validation_is_predictable(string input, bool expected) =>
        InputValidator.IsValidLocationQuery(input).Should().Be(expected);

    [Fact]
    public void Analytics_calculates_cost_correlation_and_anomalies()
    {
        var data = Enumerable.Range(0, 100).Select(i => Point(i, i == 50 ? 150 : 20 + i % 10)).ToArray();
        var result = new AnalyticsService().Calculate(data);
        result.TotalUsageKwh.Should().BeGreaterThan(2_000);
        result.MedianUsageKwh.Should().BeInRange(20, 30);
        result.Anomalies.Should().Contain(x => x.Date == DateOnly.FromDateTime(DateTime.Today).AddDays(50));
    }

    [Fact]
    public void Forecast_rejects_insufficient_data()
    {
        var result = new LinearRegressionForecastService().Train(Enumerable.Range(0, 20).Select(i => Point(i, 20)).ToArray());
        result.IsReliable.Should().BeFalse();
        result.Predictions.Should().BeEmpty();
    }

    private static EnergyDataPoint Point(int day, double usage) => new(
        DateOnly.FromDateTime(DateTime.Today).AddDays(day), usage, (decimal)usage * .15m, .15m,
        45 + day % 30, 35, 65, 40, 1800, 3, HeatingType.HeatPump, 2, true);
}
