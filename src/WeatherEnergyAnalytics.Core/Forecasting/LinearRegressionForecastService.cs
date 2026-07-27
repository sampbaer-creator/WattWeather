using WeatherEnergyAnalytics.Core.Contracts;
using WeatherEnergyAnalytics.Core.Models;

namespace WeatherEnergyAnalytics.Core.Forecasting;

public sealed record ForecastMetrics(double Mae, double Rmse, double RSquared);

public sealed record EnergyPrediction(DateOnly Date, double ActualKwh, double EstimatedKwh);

public sealed record ForecastModelResult(
    bool IsReliable,
    string StatusMessage,
    ForecastMetrics? Metrics,
    IReadOnlyList<EnergyPrediction> Predictions,
    IReadOnlyDictionary<string, double> Coefficients);

public sealed class LinearRegressionForecastService : IEnergyForecastService
{
    private static readonly string[] FeatureNames =
    [
        "Intercept", "Average temperature", "Humidity", "Heating degree days",
        "Cooling degree days", "Home size", "Occupants", "Month",
        "AC hours", "Previous usage"
    ];

    public ForecastModelResult Train(IReadOnlyList<EnergyDataPoint> data)
    {
        var ordered = data.OrderBy(x => x.Date).ToArray();
        var span = ordered.Length == 0 ? 0 : ordered[^1].Date.DayNumber - ordered[0].Date.DayNumber;
        if (ordered.Length < 90 || span < 180)
        {
            return new ForecastModelResult(
                false,
                "At least 90 usable observations spanning six months are required before an estimate is shown.",
                null,
                [],
                new Dictionary<string, double>());
        }

        var split = Math.Clamp((int)(ordered.Length * 0.8), 1, ordered.Length - 1);
        var training = ordered[..split];
        var testing = ordered[split..];
        var means = CalculateFeatureMeans(training);
        var scales = CalculateFeatureScales(training, means);
        var x = BuildMatrix(training, means, scales);
        var y = training.Select(point => point.UsageKwh).ToArray();
        var coefficients = SolveRegularizedNormalEquation(x, y, 0.001);

        var predictions = new List<EnergyPrediction>(testing.Length);
        var previousUsage = training[^1].UsageKwh;
        foreach (var point in testing)
        {
            var features = Features(point, previousUsage, means, scales);
            var estimate = Math.Max(0, Dot(coefficients, features));
            predictions.Add(new EnergyPrediction(point.Date, point.UsageKwh, estimate));
            previousUsage = point.UsageKwh;
        }

        var errors = predictions.Select(x => x.EstimatedKwh - x.ActualKwh).ToArray();
        var mae = errors.Average(Math.Abs);
        var rmse = Math.Sqrt(errors.Average(error => error * error));
        var actualAverage = predictions.Average(x => x.ActualKwh);
        var totalVariance = predictions.Sum(x => Math.Pow(x.ActualKwh - actualAverage, 2));
        var residualVariance = errors.Sum(error => error * error);
        var rSquared = totalVariance == 0 ? 0 : 1 - (residualVariance / totalVariance);

        return new ForecastModelResult(
            true,
            "Estimates use a chronological 80/20 split and an explainable regularized linear regression model.",
            new ForecastMetrics(mae, rmse, rSquared),
            predictions,
            FeatureNames.Zip(coefficients).ToDictionary(pair => pair.First, pair => pair.Second));
    }

    private static double[][] BuildMatrix(EnergyDataPoint[] data, double[] means, double[] scales)
    {
        var rows = new double[data.Length][];
        var previous = data[0].UsageKwh;
        for (var index = 0; index < data.Length; index++)
        {
            rows[index] = Features(data[index], previous, means, scales);
            previous = data[index].UsageKwh;
        }

        return rows;
    }

    private static double[] Features(EnergyDataPoint point, double previousUsage, double[] means, double[] scales)
    {
        var raw = new[]
        {
            point.AverageTemperatureF,
            point.HumidityPercent,
            Math.Max(0, 65 - point.AverageTemperatureF),
            Math.Max(0, point.AverageTemperatureF - 65),
            point.HomeSizeSquareFeet,
            point.OccupantCount,
            point.Date.Month,
            point.AirConditioningHours,
            previousUsage
        };

        var result = new double[raw.Length + 1];
        result[0] = 1;
        for (var index = 0; index < raw.Length; index++)
        {
            result[index + 1] = (raw[index] - means[index]) / scales[index];
        }

        return result;
    }

    private static double[] CalculateFeatureMeans(EnergyDataPoint[] data)
    {
        return Enumerable.Range(0, 9)
            .Select(index => data.Select(point => RawFeatures(point, point.UsageKwh)[index]).Average())
            .ToArray();
    }

    private static double[] CalculateFeatureScales(EnergyDataPoint[] data, double[] means)
    {
        return Enumerable.Range(0, 9)
            .Select(index =>
            {
                var scale = Math.Sqrt(data
                    .Select(point => Math.Pow(RawFeatures(point, point.UsageKwh)[index] - means[index], 2))
                    .Average());
                return scale < 1e-9 ? 1 : scale;
            })
            .ToArray();
    }

    private static double[] RawFeatures(EnergyDataPoint point, double previousUsage) =>
    [
        point.AverageTemperatureF,
        point.HumidityPercent,
        Math.Max(0, 65 - point.AverageTemperatureF),
        Math.Max(0, point.AverageTemperatureF - 65),
        point.HomeSizeSquareFeet,
        point.OccupantCount,
        point.Date.Month,
        point.AirConditioningHours,
        previousUsage
    ];

    private static double[] SolveRegularizedNormalEquation(double[][] x, double[] y, double lambda)
    {
        var columns = x[0].Length;
        var matrix = new double[columns, columns + 1];

        for (var row = 0; row < columns; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                matrix[row, column] = x.Sum(values => values[row] * values[column]);
            }

            matrix[row, row] += row == 0 ? 0 : lambda;
            matrix[row, columns] = x.Select((values, index) => values[row] * y[index]).Sum();
        }

        for (var pivot = 0; pivot < columns; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < columns; row++)
            {
                if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[best, pivot]))
                {
                    best = row;
                }
            }

            for (var column = pivot; column <= columns; column++)
            {
                (matrix[pivot, column], matrix[best, column]) = (matrix[best, column], matrix[pivot, column]);
            }

            var divisor = Math.Abs(matrix[pivot, pivot]) < 1e-10 ? 1e-10 : matrix[pivot, pivot];
            for (var column = pivot; column <= columns; column++)
            {
                matrix[pivot, column] /= divisor;
            }

            for (var row = 0; row < columns; row++)
            {
                if (row == pivot)
                {
                    continue;
                }

                var factor = matrix[row, pivot];
                for (var column = pivot; column <= columns; column++)
                {
                    matrix[row, column] -= factor * matrix[pivot, column];
                }
            }
        }

        return Enumerable.Range(0, columns).Select(row => matrix[row, columns]).ToArray();
    }

    private static double Dot(double[] left, double[] right) =>
        left.Zip(right).Sum(pair => pair.First * pair.Second);
}
