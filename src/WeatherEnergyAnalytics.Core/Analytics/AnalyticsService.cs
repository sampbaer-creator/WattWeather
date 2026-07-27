using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;

namespace WeatherEnergyAnalytics.Core.Analytics;

public sealed record UsageAnomaly(DateOnly Date, double UsageKwh, string Explanation);

public sealed record AnalyticsSummary(
    double TotalUsageKwh,
    double AverageDailyUsageKwh,
    double MedianUsageKwh,
    double MinimumUsageKwh,
    double MaximumUsageKwh,
    double StandardDeviationKwh,
    decimal TotalCost,
    decimal AverageCostPerKwh,
    double EstimatedMonthlyUsageKwh,
    decimal EstimatedMonthlyCost,
    double TemperatureUsageCorrelation,
    double HeatingDegreeDays,
    double CoolingDegreeDays,
    double? MonthOverMonthChangePercent,
    IReadOnlyList<UsageAnomaly> Anomalies);

public sealed class AnalyticsService : IAnalyticsService
{
    private const double DegreeDayBaseF = 65;

    public AnalyticsSummary Calculate(IReadOnlyCollection<EnergyDataPoint> data)
    {
        if (data.Count == 0)
        {
            return new AnalyticsSummary(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, []);
        }

        var ordered = data.OrderBy(x => x.Date).ToArray();
        var usage = ordered.Select(x => x.UsageKwh).OrderBy(x => x).ToArray();
        var average = usage.Average();
        var median = usage.Length % 2 == 0
            ? (usage[(usage.Length / 2) - 1] + usage[usage.Length / 2]) / 2
            : usage[usage.Length / 2];
        var variance = usage.Length > 1
            ? usage.Sum(x => Math.Pow(x - average, 2)) / (usage.Length - 1)
            : 0;

        var totalCost = ordered.Sum(x => x.Cost);
        var rate = ordered.Where(x => x.CostPerKwh > 0).Select(x => x.CostPerKwh).DefaultIfEmpty().Average();
        var recent = ordered.TakeLast(Math.Min(30, ordered.Length)).ToArray();
        var estimatedUsage = recent.Average(x => x.UsageKwh) * 30.4375;
        var estimatedCost = recent.Average(x => x.Cost) * 30.4375m;

        var hdd = ordered.Sum(x => Math.Max(0, DegreeDayBaseF - x.AverageTemperatureF));
        var cdd = ordered.Sum(x => Math.Max(0, x.AverageTemperatureF - DegreeDayBaseF));

        return new AnalyticsSummary(
            usage.Sum(),
            average,
            median,
            usage[0],
            usage[^1],
            Math.Sqrt(variance),
            totalCost,
            rate,
            estimatedUsage,
            estimatedCost,
            Correlation(ordered.Select(x => x.AverageTemperatureF).ToArray(), ordered.Select(x => x.UsageKwh).ToArray()),
            hdd,
            cdd,
            CalculateMonthOverMonth(ordered),
            FindIqrAnomalies(ordered));
    }

    private static IReadOnlyList<UsageAnomaly> FindIqrAnomalies(EnergyDataPoint[] data)
    {
        if (data.Length < 8)
        {
            return [];
        }

        var sorted = data.Select(x => x.UsageKwh).OrderBy(x => x).ToArray();
        var q1 = Percentile(sorted, 0.25);
        var q3 = Percentile(sorted, 0.75);
        var iqr = q3 - q1;
        var lower = q1 - (1.5 * iqr);
        var upper = q3 + (1.5 * iqr);

        return data
            .Where(x => x.UsageKwh < lower || x.UsageKwh > upper)
            .Select(x => new UsageAnomaly(
                x.Date,
                x.UsageKwh,
                $"Usage falls outside the explainable IQR range of {lower:F1}–{upper:F1} kWh. This is unusual, not necessarily incorrect."))
            .ToArray();
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var position = (sorted.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper)
        {
            return sorted[lower];
        }

        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }

    private static double Correlation(double[] x, double[] y)
    {
        if (x.Length < 2 || x.Length != y.Length)
        {
            return 0;
        }

        var averageX = x.Average();
        var averageY = y.Average();
        var numerator = x.Zip(y).Sum(pair => (pair.First - averageX) * (pair.Second - averageY));
        var denominator = Math.Sqrt(
            x.Sum(value => Math.Pow(value - averageX, 2)) *
            y.Sum(value => Math.Pow(value - averageY, 2)));

        return denominator == 0 ? 0 : numerator / denominator;
    }

    private static double? CalculateMonthOverMonth(EnergyDataPoint[] ordered)
    {
        var months = ordered
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .OrderBy(x => x.Key.Year)
            .ThenBy(x => x.Key.Month)
            .Select(group => group.Sum(x => x.UsageKwh))
            .TakeLast(2)
            .ToArray();

        return months.Length == 2 && months[0] != 0
            ? ((months[1] - months[0]) / months[0]) * 100
            : null;
    }
}
