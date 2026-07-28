# WattWeather

### Know whether solar makes sense where you live.

WattWeather is a C# weather and energy dashboard that turns one U.S. city search into a clear local snapshot: solar potential, weather-driven energy pressure, state power sources, available discounts, and household energy trends.

[Open the live app](https://sampbaer-creator.github.io/WattWeather/) · [Explore solar](https://sampbaer-creator.github.io/WattWeather/solar) · [Check discounts](https://sampbaer-creator.github.io/WattWeather/discounts)

## What WattWeather answers

The experience is divided into focused pages so visitors do not have to navigate one crowded dashboard.

| Page | What it helps answer |
| --- | --- |
| **Overview** | What is this city's weather and energy snapshot? |
| **Solar** | What could a typical 6 kW system with 15 solar panels produce? |
| **Discounts** | Which confirmed rebates could reduce a solar quote? |
| **Weather** | Are local temperatures increasing heating or cooling demand? |
| **Power** | Which energy source typically leads this state's electricity generation? |
| **My energy** | How are a household's bills, usage, and temperatures related over time? |

Solar production, savings, weather relationships, and discounts are screening estimates—not quotes, guarantees, tax advice, or substitutes for current program rules.

## Design

WattWeather uses a warm, approachable visual system inspired by sunlight and changing weather.

| Role | Color |
| --- | --- |
| Canvas | `#fffafa` |
| Solar yellow | `#ffc928` |
| Deep navy | `#10243e` |
| Action coral | `#ff6846` |
| Sky blue | `#83d7f5` |
| Energy green | `#83d19c` |
| Supporting violet | `#a98bf2` |

The interface uses rounded cards, circular solar graphics, high-contrast calls to action, responsive navigation, and plain-language explanations. CTA colors intentionally differ from the company colors while staying within the weather-and-solar theme.

## Solar and discount tools

The Solar page models a practical baseline system:

- 15 solar panels at 400 watts each
- 6 kW total array size
- An 80% real-world production factor for heat, wiring, inverter, shade, and other losses
- Local solar radiation and state residential electricity prices

The Discounts page explains current federal status, links to [DSIRE](https://programs.dsireusa.org/system/program) for state and utility programs, and includes a calculator for rebates that the visitor has independently confirmed. It also links to the external [Solar Estimate savings calculator](https://www.solar-estimate.org/savings-calculator) for comparison.

## Architecture

WattWeather keeps the main application logic in C#:

```text
app/       .NET 10 Blazor WebAssembly UI and energy calculations
server/    ASP.NET Core API, Open-Meteo proxy, EIA data, and security middleware
tests/     xUnit tests for calculations and backend security
scripts/   Monthly EIA snapshot refresh
```

The secure ASP.NET edition uses same-origin `/api` endpoints. The server validates inputs, rate-limits API traffic, caches public responses, compresses output, limits request bodies, removes identifying server headers, and sends CSP, HSTS, frame, MIME-sniffing, referrer, permissions, and cross-origin isolation headers.

The live GitHub Pages edition is a standalone Blazor WebAssembly build. GitHub Pages cannot execute ASP.NET Core, so this edition reads public Open-Meteo endpoints and the checked-in EIA snapshot directly. Personal energy records stay in browser local storage and are not uploaded.

## Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```powershell
dotnet restore WattWeather.slnx
dotnet run --project server/WattWeather.Server.csproj
```

Open the local address printed by ASP.NET Core.

## Verify

```powershell
dotnet build WattWeather.slnx -c Release
dotnet test WattWeather.slnx -c Release
dotnet publish server/WattWeather.Server.csproj -c Release -o artifacts/publish
```

GitHub Actions builds, tests, and publishes the application. A separate monthly workflow refreshes `server/Data/eia-state-energy.json` when an EIA API secret is configured.

## Deployment

Every push to `main` triggers `.github/workflows/pages.yml`, which publishes the standalone Blazor application to GitHub Pages. The workflow configures the `/WattWeather/` base path and route fallback so direct page links work.

The secure backend edition can be deployed using the included `Dockerfile` and `render.yaml`:

1. Open [Render's New Blueprint page](https://dashboard.render.com/blueprints).
2. Connect `sampbaer-creator/WattWeather`.
3. Apply the detected `render.yaml`.

The Docker image runs as a non-root user, exposes a health endpoint at `/health`, and listens on container port `8080`.

## Public data

- [Open-Meteo](https://open-meteo.com/) supplies city search, current conditions, forecasts, solar radiation, and historical temperature.
- [U.S. Energy Information Administration](https://www.eia.gov/opendata/) supplies state residential prices, usage statistics, and electricity-generation data.
- [DSIRE](https://programs.dsireusa.org/system/program) and [IRS Residential Clean Energy Credit guidance](https://www.irs.gov/credits-deductions/residential-clean-energy-credit) support program research. Eligibility and current rules must always be verified with the responsible agency or utility.
