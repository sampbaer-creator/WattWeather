# WattWeather

### Is the weather driving up your electric bill?

WattWeather is a focused C# and Blazor application that compares local weather with statewide residential electricity averages, then lets visitors privately test whether temperature helps explain their own daily electricity use.

[Open WattWeather](https://sampbaer-creator.github.io/WattWeather/) · [My Energy](https://sampbaer-creator.github.io/WattWeather/energy) · [Architecture](https://sampbaer-creator.github.io/WattWeather/architecture)

## The three-step experience

| Page | Purpose |
| --- | --- |
| **Overview** | Search a U.S. city, view current climate pressure, compare statewide usage and price, and check one bill. |
| **My Energy** | Add or import private daily records, calculate Pearson correlation, and flag unusually high usage. |
| **Architecture** | Review the Blazor, ASP.NET, Open-Meteo, EIA, local-storage, security, and testing design. |

WattWeather intentionally does not estimate solar ROI, roofs, tax credits, incentives, or rebates. Its single product question is: **does weather help explain your electricity bill?**

## Data boundaries

- Open-Meteo supplies U.S. city search, current weather, apparent temperature, and historical daily temperature.
- U.S. EIA data supplies statewide residential electricity price and household usage averages.
- City weather is local; electricity averages are statewide.
- Public data cannot diagnose an individual bill.
- Personal energy records stay in browser `localStorage`.
- Shared links contain only `city` and `state`.
- The latest successfully loaded EIA dataset is cached locally and used when the state-data endpoint is temporarily unavailable.
- City search waits 350 milliseconds after typing stops before calling Open-Meteo.

## Analytics

- **One-bill comparison:** above average is over 10% higher than the state average, near average is within ±10%, and below average is over 10% lower.
- **Estimated usage:** when kWh is missing, the app estimates it from bill cost and the statewide residential rate and labels it as estimated.
- **Heating and cooling pressure:** current apparent temperature is compared with a 65°F baseline.
- **HDD/CDD65:** daily degrees below or above 65°F.
- **Pearson correlation:** measures linear temperature–usage association without claiming causation.
- **IQR anomaly review:** flags usage above `Q3 + 1.5 × IQR` as a review prompt, not proof of an error.
- **CSV validation:** full-history analysis requires at least three unique daily rows; weekly and monthly cadence is rejected.

## Technology

```text
app/       .NET 10 Blazor WebAssembly UI, models, local storage, and analytics
server/    Optional ASP.NET Core API, Open-Meteo proxy, EIA snapshot, and security middleware
tests/     xUnit calculation, CSV-validation, privacy, and backend-security tests
scripts/   Monthly EIA snapshot refresh
```

The live GitHub Pages edition runs as standalone Blazor WebAssembly. The optional ASP.NET Core edition provides validated, cached, rate-limited same-origin API endpoints and security headers.

## Run locally

```powershell
dotnet restore WattWeather.slnx
dotnet run --project server/WattWeather.Server.csproj
```

## Verify

```powershell
dotnet build WattWeather.slnx -c Release
dotnet test WattWeather.slnx -c Release
dotnet publish app/WattWeather.App.csproj -c Release
```

Every push to `main` runs the .NET checks and deploys the Blazor application to GitHub Pages.

## Social sharing

The static GitHub Pages edition includes a polished default Open Graph card and updates the title and description in the browser for the selected city. Social crawlers generally do not execute Blazor WebAssembly, so truly city-specific crawler previews require the optional ASP.NET host or a future edge-rendered share endpoint.
