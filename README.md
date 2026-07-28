# WattWeather

### Simple, fun weather for any U.S. city or ZIP code.

WattWeather is a focused C# and Blazor weather application. Search a U.S. city or ZIP and get current conditions, today’s details, and a seven-day forecast without creating an account.

[Open the live app](https://sampbaer-creator.github.io/WattWeather/) · [How it works](https://sampbaer-creator.github.io/WattWeather/architecture)

## Weather details

- Current temperature and “feels like”
- Weather condition and day/night icon
- Today’s high and low
- Humidity
- Wind speed and direction
- Current precipitation
- Cloud cover
- Surface pressure
- UV index
- Sunrise and sunset
- Seven-day highs, lows, rain chance, wind, and conditions

## Technology

```text
app/       .NET 10 Blazor WebAssembly UI, weather models, and API client
server/    Optional ASP.NET Core proxy, validation, caching, rate limits, and security
tests/     xUnit weather-presentation and backend-security tests
```

Open-Meteo provides U.S. geocoding and weather data. The live GitHub Pages version calls its public HTTPS endpoints directly. The optional ASP.NET edition provides same-origin API endpoints.

## Run

```powershell
dotnet restore WattWeather.slnx
dotnet run --project server/WattWeather.Server.csproj
```

## Verify

```powershell
dotnet build WattWeather.slnx -c Release
dotnet test WattWeather.slnx -c Release
```

Every push to `main` runs the checks and deploys the Blazor app to GitHub Pages.
