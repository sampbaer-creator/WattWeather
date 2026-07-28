namespace WattWeather.App.Services;

public static class WeatherPresentation
{
    public static string Label(int code) => code switch
    {
        0 => "Clear skies", 1 => "Mostly clear", 2 => "Partly cloudy", 3 => "Overcast",
        45 or 48 => "Foggy", >= 51 and <= 57 => "Drizzle", >= 61 and <= 67 => "Rain",
        >= 71 and <= 77 => "Snow", >= 80 and <= 82 => "Rain showers",
        >= 85 and <= 86 => "Snow showers", >= 95 => "Thunderstorms", _ => "Mixed weather"
    };

    public static string Emoji(int code, bool isDay = true) => code switch
    {
        0 => isDay ? "☀️" : "🌙", 1 => isDay ? "🌤️" : "🌙", 2 => "⛅", 3 => "☁️",
        45 or 48 => "🌫️", >= 51 and <= 67 => "🌧️", >= 71 and <= 77 => "❄️",
        >= 80 and <= 82 => "🌦️", >= 85 and <= 86 => "🌨️", >= 95 => "⛈️", _ => "🌡️"
    };

    public static string WindDirection(double degrees)
    {
        string[] directions = ["N", "NE", "E", "SE", "S", "SW", "W", "NW"];
        var normalized = ((degrees % 360) + 360) % 360;
        return directions[(int)Math.Round(normalized / 45) % 8];
    }
}
