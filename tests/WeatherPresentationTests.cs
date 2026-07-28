using WattWeather.App.Services;

namespace WattWeather.Tests;

public sealed class WeatherPresentationTests
{
    [Theory]
    [InlineData(0, "Clear skies")]
    [InlineData(3, "Overcast")]
    [InlineData(63, "Rain")]
    [InlineData(75, "Snow")]
    [InlineData(95, "Thunderstorms")]
    public void Label_MapsWeatherCodes(int code, string expected) =>
        Assert.Equal(expected, WeatherPresentation.Label(code));

    [Fact]
    public void Emoji_UsesMoonForClearNight()
    {
        Assert.Equal("🌙", WeatherPresentation.Emoji(0, false));
        Assert.Equal("☀️", WeatherPresentation.Emoji(0, true));
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(45, "NE")]
    [InlineData(180, "S")]
    [InlineData(270, "W")]
    [InlineData(359, "N")]
    [InlineData(-90, "W")]
    public void WindDirection_HandlesCompassAndNormalization(double degrees, string expected) =>
        Assert.Equal(expected, WeatherPresentation.WindDirection(degrees));

    [Fact]
    public void ForecastUrl_RequestsSevenDayWeatherDetails()
    {
        var url = WeatherService.BuildOpenMeteoUrl(39.73915, -104.9847);

        Assert.Contains("forecast_days=7", url);
        Assert.Contains("sunrise", url);
        Assert.Contains("uv_index_max", url);
        Assert.Contains("precipitation_probability_max", url);
        Assert.DoesNotContain("electric", url, StringComparison.OrdinalIgnoreCase);
    }
}
