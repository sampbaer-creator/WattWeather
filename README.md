# WattWeather

WattWeather turns public weather and electricity data into simple answers about solar panels, energy use, and local power.

**Live app:** https://sampbaer-creator.github.io/WattWeather/

## What you can explore

| Page | Question it answers |
| --- | --- |
| [Overview](https://sampbaer-creator.github.io/WattWeather/) | What is my city's weather and energy snapshot? |
| [Solar value](https://sampbaer-creator.github.io/WattWeather/solar.html) | Is a typical 6 kW, 15-panel solar system worth exploring here? |
| [Solar discounts](https://sampbaer-creator.github.io/WattWeather/incentives.html) | Which rebates or programs could reduce the cost? |
| [Weather impact](https://sampbaer-creator.github.io/WattWeather/weather-impact.html) | Could hot or cold weather increase energy demand? |
| [Power sources](https://sampbaer-creator.github.io/WattWeather/power.html) | Which electricity source typically leads my state? |
| [My energy](https://sampbaer-creator.github.io/WattWeather/energy.html) | How does my own electricity use relate to weather? |

The Discounts page explains the current federal homeowner-credit status, common state and utility programs, and includes a calculator for rebates the visitor has confirmed.

## How it works

- Open-Meteo supplies city search, current conditions, forecasts, solar radiation, and historical temperature.
- The U.S. Energy Information Administration supplies state residential electricity price and usage statistics.
- A monthly GitHub Actions workflow refreshes the checked-in EIA snapshot without exposing an API key to visitors.
- Personal bill records and analysis stay in the visitor's browser through local storage.
- The app uses plain HTML, CSS, and JavaScript. It has no framework, package installation, server runtime, database, or production build step.

Solar output, savings, weather relationships, and discounts are screening estimates—not installer quotes, guarantees, or tax advice.

## Run locally

From the repository root:

```powershell
python -m http.server 8080 --bind 127.0.0.1
```

Open http://127.0.0.1:8080/.

## Verify changes

Node.js is needed only for repository checks and the EIA refresh script:

```powershell
node --check dashboard.js
node --check pages.js
node scripts/validate-site.mjs
```

The validator checks all six pages, internal routes, referenced assets, duplicate element IDs, and state-data coverage. GitHub runs the same checks on every push and pull request.

## Project structure

```text
index.html
solar.html
incentives.html
weather-impact.html
power.html
energy.html

dashboard.css / dashboard.js       Shared public dashboard UI and logic
pages.css / pages-extra.css        Personal-energy workspace styles
pages.js                           Personal-energy analysis
data/eia-state-energy.json         Cached EIA state statistics
scripts/fetch-eia-data.mjs         Secure monthly data refresh
scripts/validate-site.mjs          Dependency-free repository checks
.github/workflows/                 Validation and EIA refresh automation
```

## Deployment

GitHub Pages serves the repository directly. Because the app is static, deploying an update only requires committing and pushing the changed files to the configured Pages branch.
