# Contributing to WattWeather

Thank you for helping improve WattWeather. Contributions that make weather and
electricity data easier for everyday users to understand are especially welcome.

## Ways to contribute

- Report a reproducible bug.
- Suggest an analytics or accessibility improvement.
- Improve documentation or data-source explanations.
- Add deterministic tests.
- Propose a focused code change.

Never include API keys, utility account details, addresses, or other private
information in issues, screenshots, test data, or commits.

## Before opening an issue

1. Search existing issues for the same problem or idea.
2. Confirm it still applies to the live site or current `main` branch.
3. Remove personal energy information and secrets from screenshots and logs.
4. Include the browser, operating system, and exact reproduction steps.

## Development setup

Serve the static application:

```powershell
python -m http.server 8080
```

Run the .NET web application:

```powershell
dotnet run --project src/WeatherEnergyAnalytics.Web
```

Run tests:

```powershell
dotnet test tests/WeatherEnergyAnalytics.Core.Tests -c Release
dotnet test tests/WeatherEnergyAnalytics.Infrastructure.Tests -c Release
```

## Pull requests

- Keep each pull request focused on one meaningful change.
- Explain what changed, why it matters, and how it was tested.
- Add or update tests when changing calculations or forecasting behavior.
- Update documentation when changing a data source, metric, or workflow.
- Never commit API keys or real household utility records.

