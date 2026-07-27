using System.Text.RegularExpressions;

namespace WeatherEnergyAnalytics.Core.Validation;

public static partial class InputValidator
{
    [GeneratedRegex(@"^\d{5}(?:-\d{4})?$")]
    private static partial Regex UsZipCodeRegex();

    [GeneratedRegex(@"^[\p{L}\p{M} .,'-]{2,120}(?:,\s*[\p{L}]{2,80})?$")]
    private static partial Regex CityRegex();

    public static bool IsValidLocationQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return UsZipCodeRegex().IsMatch(normalized) || CityRegex().IsMatch(normalized);
    }

    public static bool IsValidUsZip(string? value) =>
        !string.IsNullOrWhiteSpace(value) && UsZipCodeRegex().IsMatch(value.Trim());

    public static string NormalizeLocationQuery(string value)
    {
        if (!IsValidLocationQuery(value))
        {
            throw new ValidationException("Enter a five-digit US ZIP code or a city name such as Orem, UT.");
        }

        return Regex.Replace(value.Trim(), @"\s+", " ");
    }
}

public sealed class ValidationException(string message) : Exception(message);
