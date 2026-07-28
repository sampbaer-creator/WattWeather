# WattWeather

### Does hot or cold weather help explain your electricity bill?

WattWeather is a C# weather-and-energy analytics application. One U.S. city search combines local Open-Meteo weather with statewide EIA household electricity averages; a separate, browser-private workflow lets a visitor compare one bill or analyze daily usage history.

[Open the live app](https://sampbaer-creator.github.io/WattWeather/) · [Review the architecture](https://sampbaer-creator.github.io/WattWeather/architecture) · [Analyze daily energy](https://sampbaer-creator.github.io/WattWeather/energy)

## What it answers

| Page | Question |
| --- | --- |
| **Overview** | How does local weather compare with statewide electricity use and price, and where does one bill sit? |
| **My energy** | Does temperature have a measurable relationship with private daily usage? |
| **Weather** | Are current conditions adding heating or cooling pressure? |
| **Power** | Which source typically leads the state's electricity generation? |
| **Solar** | Is a representative 6 kW system worth investigating further? |
| **Discounts** | Where can a visitor research incentives and model confirmed rebates? |
| **Architecture** | How do the Blazor WebAssembly and optional ASP.NET editions work? |

The public snapshot does **not** provide city-level household electricity measurements: weather is local, while electricity use and price are statewide EIA averages. Solar production, weather relationships, forecasts, and discounts are screening estimates—not quotes, guarantees, causal claims, tax advice, or substitutes for current program rules.

## Validated analytics

- **One-bill comparison:** above average is more than 10% over the state household average; near average is within ±10%; below average is more than 10% under. If kWh is omitted, usage is explicitly estimated from bill cost and the state average residential rate.
- **Full-history import:** requires at least three unique daily rows. Weekly or monthly cadence is rejected instead of being silently matched to daily weather.
- **Pearson correlation:** measures linear association between temperature and electricity use; it does not establish causation.
- **HDD/CDD65:** sums daily degrees below or above a 65°F reference baseline.
- **IQR anomaly review:** flags values above `Q3 + 1.5 × IQR`; this robust method does not assume normally distributed household use.
- **Regression:** explainable linear regression is preferred for estimates because limited household datasets do not justify an overfit-prone model.

Share links contain only `city` and `state`. Personal kWh and cost records remain in local storage and never enter the public URL.

## Supporting solar and discount tools

The solar screen models a representative 15-panel, 6 kW array with an 80% production factor, local forecast radiation, and the statewide residential price. It is not a roof design or financial guarantee.

The Discounts page links to [DSIRE](https://programs.dsireusa.org/system/program) and [IRS guidance](https://www.irs.gov/credits-deductions/residential-clean-energy-credit), then lets visitors model rebates they have independently confirmed. WattWeather does not claim live incentive eligibility or DSIRE API integration.

## Architecture

```text
app/       .NET 10 Blazor WebAssembly UI and analytics
server/    Optional ASP.NET Core API, Open-Meteo proxy, EIA data, and security middleware
tests/     xUnit calculation, import-validation, and backend-security tests
scripts/   Monthly EIA snapshot refresh
```

The live GitHub Pages edition runs as standalone Blazor WebAssembly, calls public Open-Meteo endpoints, and reads a checked-in EIA snapshot. The optional server edition provides validated, cached, rate-limited same-origin `/api` endpoints plus security headers. GitHub Pages cannot execute ASP.NET Core.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then:

```powershell
dotnet restore WattWeather.slnx
dotnet run --project server/WattWeather.Server.csproj
```

## Verify

```powershell
dotnet build WattWeather.slnx -c Release
dotnet test WattWeather.slnx -c Release
dotnet publish server/WattWeather.Server.csproj -c Release -o artifacts/publish
```

The deterministic suite covers bill-result branches, cost-only estimation, Pearson behavior, HDD/CDD65, IQR summaries, daily CSV cadence, solar calculations, and ASP.NET security behavior.

## Deployment

Every push to `main` triggers `.github/workflows/pages.yml`, publishing the standalone Blazor application at the `/WattWeather/` base path. The optional ASP.NET edition can be deployed with the included `Dockerfile` and `render.yaml`.

## Data sources

- [Open-Meteo](https://open-meteo.com/): city search, current conditions, forecasts, solar radiation, and historical temperature.
- [U.S. Energy Information Administration](https://www.eia.gov/opendata/): statewide residential prices, usage, and generation data.
- [DSIRE](https://programs.dsireusa.org/system/program) and [IRS](https://www.irs.gov/credits-deductions/residential-clean-energy-credit): external program-research guidance.

## Status and limitations

Active portfolio project. State averages are context, not household or city measurements. Correlation does not prove cause. One bill cannot establish a pattern. Forecasts and anomalies are estimates and review prompts.
