# WattWeather

WattWeather is a C# energy dashboard that answers one practical question at a time: whether solar is worth exploring locally, how weather affects demand, what powers a state, which confirmed discounts reduce a quote, and how a household's own usage changes over time.

The interface is intentionally split into focused pages instead of one crowded dashboard:

| Page | Question |
| --- | --- |
| Overview | What is this city's weather and energy snapshot? |
| Solar | What could a typical 6 kW, 15-panel system produce? |
| Discounts | Which confirmed rebates reduce a solar quote? |
| Weather | Are current temperatures pushing heating or cooling demand? |
| Power | Which source typically leads this state's electricity generation? |
| My energy | What patterns exist in the visitor's own bill records? |

Solar output, savings, weather relationships, and discounts are screening estimates—not quotes, guarantees, tax advice, or a substitute for current program rules.

## Architecture

- `app/` — .NET 10 Blazor WebAssembly UI and C# calculations
- `server/` — ASP.NET Core API, Open-Meteo proxy, cached EIA data, security middleware
- `tests/` — xUnit coverage for the core calculation rules
- `scripts/` — monthly EIA snapshot refresh
- `Dockerfile` — reproducible non-root production image
- `render.yaml` — health-checked Render deployment with deploys gated on passing CI

The browser calls only same-origin `/api` endpoints. The server validates inputs, hides upstream implementation details, applies per-IP API rate limits, caches public responses, compresses output, limits request bodies, removes the Kestrel server header, and sends CSP, HSTS, frame, MIME-sniffing, referrer, permissions, and cross-origin isolation headers. Personal energy records remain in browser local storage and are not uploaded.

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

GitHub Actions runs the same build, test, and publish checks on pushes and pull requests. A separate monthly workflow refreshes `server/Data/eia-state-energy.json` using the repository's EIA secret.

## Deploy

The app requires an ASP.NET Core host; GitHub Pages cannot run its backend. The checked-in Render Blueprint builds the Docker image, checks `/health`, and deploys only after CI succeeds.

1. Open [Render's New Blueprint page](https://dashboard.render.com/blueprints).
2. Connect `sampbaer-creator/WattWeather`.
3. Apply the detected `render.yaml`.

After that first connection, pushes to the selected branch deploy automatically after GitHub checks pass. The same Dockerfile can be used on any container host that supplies HTTPS at the edge and maps its public port to container port `8080`.

## Public data

- [Open-Meteo](https://open-meteo.com/) provides city search, current conditions, forecasts, solar radiation, and historical temperature.
- [U.S. Energy Information Administration](https://www.eia.gov/opendata/) provides state residential price and usage statistics.
- [DSIRE](https://programs.dsireusa.org/system/program) and current IRS guidance are linked for program research; eligibility must be verified with the responsible agency or utility.
