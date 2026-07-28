using WattWeather.App.Models;

namespace WattWeather.App.Services;

public sealed class EnergyCalculations
{
    private const double SystemSizeKilowatts = 6;
    private const double SystemEfficiency = 0.80;

    private static readonly Dictionary<string, PowerProfile> StatePower = BuildStatePowerProfiles();

    public SolarEstimate CalculateSolar(WeatherForecast forecast, StateEnergy? state)
    {
        var dailySolar = forecast.Daily.SolarRadiationMegajoules.Select(value => value / 3.6).ToList();
        var average = dailySolar.Count == 0 ? 0 : dailySolar.Average();
        var price = state?.ResidentialPriceCents ?? 15;
        var adjusted = average + (price - 15) * 0.025;
        var (score, verdict, explanation) = adjusted switch
        {
            >= 5 => (Math.Min(96, (int)Math.Round(72 + adjusted * 4)), "Strong solar signal", "Local sunlight makes rooftop solar worth a closer quote."),
            >= 3.5 => ((int)Math.Round(48 + adjusted * 5), "Worth exploring", "The solar resource looks promising; roof and utility details will decide the economics."),
            _ => (Math.Max(28, (int)Math.Round(26 + adjusted * 6)), "Conditional fit", "Roof exposure, incentives, and electricity price matter more here.")
        };

        var annualOutput = SystemSizeKilowatts * average * 365 * SystemEfficiency;
        return new SolarEstimate(average, annualOutput, annualOutput * price / 100, score, verdict, explanation);
    }

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

    public static DiscountEstimate CalculateDiscount(decimal quote, decimal stateOrLocal, decimal utility)
    {
        quote = Math.Max(0, quote);
        var discounts = Math.Min(quote, Math.Max(0, stateOrLocal) + Math.Max(0, utility));
        var net = quote - discounts;
        var percent = quote == 0 ? 0 : discounts / quote * 100;
        return new DiscountEstimate(quote, discounts, net, percent);
    }

    public static EnergySummary Summarize(IReadOnlyCollection<EnergyRecord> records)
    {
        if (records.Count == 0)
        {
            return new EnergySummary(0, 0, 0, 0, null, 0);
        }

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
        if (pairs.Count < 3)
        {
            return null;
        }

        var meanX = pairs.Average(pair => pair.X);
        var meanY = pairs.Average(pair => pair.Y);
        var numerator = pairs.Sum(pair => (pair.X - meanX) * (pair.Y - meanY));
        var denominator = Math.Sqrt(
            pairs.Sum(pair => Math.Pow(pair.X - meanX, 2)) *
            pairs.Sum(pair => Math.Pow(pair.Y - meanY, 2)));
        return denominator == 0 ? null : numerator / denominator;
    }

    public PowerProfile GetPowerProfile(string? state)
    {
        return state is not null && StatePower.TryGetValue(state, out var profile)
            ? profile
            : new PowerProfile("Regional grid mix", "Mixed", "GRID", "Electricity moves across state lines and the generation mix changes throughout the day.");
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

    private static double Percentile(IReadOnlyList<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return values[(int)Math.Floor((values.Count - 1) * percentile)];
    }

    private static Dictionary<string, PowerProfile> BuildStatePowerProfiles()
    {
        var profiles = new Dictionary<string, PowerProfile>(StringComparer.OrdinalIgnoreCase);
        Add(profiles, ["Idaho", "Maine", "Oregon", "Vermont", "Washington"], "Hydropower", "Renewable", "H₂O", "Flowing water is the state's typical leading electricity resource. Output can shift with snowpack, rainfall, and reservoirs.");
        Add(profiles, ["Colorado", "Iowa", "Kansas", "Minnesota", "New Mexico", "Oklahoma"], "Wind", "Renewable", "AIR", "Wind is the state's typical leading resource. The wider grid balances changing wind conditions with other sources.");
        Add(profiles, ["Connecticut", "Illinois", "Maryland", "New Hampshire", "South Carolina", "Tennessee"], "Nuclear", "Nuclear", "ATOM", "Nuclear plants typically lead generation and provide steady output across most weather conditions.");
        Add(profiles, ["Indiana", "Kentucky", "Missouri", "Montana", "Nebraska", "North Dakota", "Utah", "West Virginia", "Wyoming"], "Coal", "Fossil", "COAL", "Coal remains the state's typical leading source. It is dispatchable but comparatively carbon intensive.");
        Add(profiles, ["Hawaii"], "Petroleum", "Fossil", "OIL", "Petroleum typically leads this island grid, where fuel transport contributes to higher electricity prices.");
        Add(profiles,
            ["Alabama", "Alaska", "Arizona", "Arkansas", "California", "Delaware", "Florida", "Georgia", "Louisiana", "Massachusetts", "Michigan", "Mississippi", "Nevada", "New Jersey", "New York", "North Carolina", "Ohio", "Pennsylvania", "Rhode Island", "Texas", "Virginia", "Wisconsin", "District of Columbia"],
            "Natural gas", "Fossil", "GAS", "Natural gas typically leads generation and can respond quickly when weather pushes demand higher.");
        return profiles;
    }

    private static void Add(
        IDictionary<string, PowerProfile> profiles,
        IEnumerable<string> states,
        string name,
        string category,
        string symbol,
        string description)
    {
        foreach (var state in states)
        {
            profiles[state] = new PowerProfile(name, category, symbol, description);
        }
    }
}
