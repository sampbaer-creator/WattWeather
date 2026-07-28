using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class EnergyCalculations
{
    public static DemandEstimate CalculateDemand(double feelsLike)
    {
        var heating = Math.Max(0, 65 - feelsLike);
        var cooling = Math.Max(0, feelsLike - 65);
        var summary = heating > 12
            ? "Cold weather is likely adding meaningful heating demand."
            : cooling > 12
                ? "Hot weather is likely adding meaningful cooling demand."
                : "Current weather is close to the 65°F comfort baseline.";
        return new DemandEstimate(heating, cooling, summary);
    }

    public static DegreeDaySummary CalculateDegreeDays(IEnumerable<double> meanTemperatures, double baseline = 65)
    {
        var temperatures = meanTemperatures.ToList();
        return new DegreeDaySummary(
            temperatures.Sum(value => Math.Max(0, baseline - value)),
            temperatures.Sum(value => Math.Max(0, value - baseline)));
    }

    public static BillComparison CompareBill(decimal totalCost, double? kilowattHours, StateEnergy state)
    {
        if (totalCost < 0 || (kilowattHours.HasValue && kilowattHours <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(totalCost), "Enter a valid bill cost and optional kWh value.");
        }
        if (state.AverageMonthlyKwh <= 0 || state.ResidentialPriceCents <= 0)
        {
            throw new InvalidOperationException("State comparison data is unavailable.");
        }

        var estimated = !kilowattHours.HasValue;
        var usage = kilowattHours ?? (double)totalCost / (state.ResidentialPriceCents / 100);
        var difference = (usage - state.AverageMonthlyKwh) / state.AverageMonthlyKwh * 100;
        var band = difference > 10 ? BillComparisonBand.Above :
            difference < -10 ? BillComparisonBand.Below : BillComparisonBand.Near;
        var (headline, guidance) = band switch
        {
            BillComparisonBand.Above => ("Above the state household average", "Your bill is worth exploring further to see what drove the difference."),
            BillComparisonBand.Below => ("Below the state household average", "See whether this lower-use pattern holds across more months."),
            _ => ("Near the state household average", "Weather may not be the main driver for this billing period.")
        };
        return new BillComparison(usage, estimated, difference, band, headline, guidance);
    }

    public static string BuildPublicShareQuery(CityLocation city) =>
        $"?city={Uri.EscapeDataString(city.Name)}&state={Uri.EscapeDataString(city.State ?? "")}";

    public static EnergySummary Summarize(IReadOnlyCollection<EnergyRecord> records)
    {
        if (records.Count == 0) return new EnergySummary(0, 0, 0, 0, null, 0);

        var total = records.Sum(record => record.KilowattHours);
        var values = records.Select(record => record.KilowattHours).Order().ToList();
        var lower = Percentile(values, 0.25);
        var upper = Percentile(values, 0.75);
        var highFence = upper + 1.5 * (upper - lower);
        var pairs = records
            .Where(record => record.MeanTemperature.HasValue)
            .Select(record => (X: record.MeanTemperature!.Value, Y: record.KilowattHours))
            .ToList();

        return new EnergySummary(
            records.Count,
            total,
            total / records.Count,
            records.Sum(record => record.Cost ?? 0),
            Pearson(pairs),
            records.Count(record => record.KilowattHours > highFence));
    }

    public static double? Pearson(IReadOnlyCollection<(double X, double Y)> pairs)
    {
        if (pairs.Count < 3) return null;
        var meanX = pairs.Average(pair => pair.X);
        var meanY = pairs.Average(pair => pair.Y);
        var numerator = pairs.Sum(pair => (pair.X - meanX) * (pair.Y - meanY));
        var denominator = Math.Sqrt(
            pairs.Sum(pair => Math.Pow(pair.X - meanX, 2)) *
            pairs.Sum(pair => Math.Pow(pair.Y - meanY, 2)));
        return denominator == 0 ? null : numerator / denominator;
    }

    public static string WeatherLabel(int code) => code switch
    {
        0 => "Clear skies",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        >= 51 and <= 55 => "Drizzle",
        >= 61 and <= 65 => "Rain",
        >= 71 and <= 77 => "Snow",
        >= 80 and <= 82 => "Rain showers",
        >= 95 => "Thunderstorm",
        _ => "Current conditions"
    };

    private static double Percentile(IReadOnlyList<double> values, double percentile) =>
        values.Count == 0 ? 0 : values[(int)Math.Floor((values.Count - 1) * percentile)];
}
