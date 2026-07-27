# Roadmap, lessons, and resume bullets

## Roadmap

- Add full energy add/edit/delete forms to both clients.
- Add saved-location persistence after each live request.
- Integrate Open-Meteo archive imports with explicit provenance.
- Add native MAUI charts and mobile targets.
- Add EF Core migrations, accessibility QA, screenshots, and end-to-end browser tests.
- Deploy the server-side web app to Azure App Service or another .NET host.

## Challenges and lessons learned

The original class assignment coupled API access, JSON parsing, global state, UI updates, and navigation in one event handler. Separating these concerns makes error handling and automated testing practical. Historical weather availability also depends on vendor plan, so provenance is modeled explicitly rather than silently mixing live, imported, and synthetic data. A chronological model split better represents future prediction than random sampling.

## Suggested resume bullets

- Re-architected a .NET MAUI weather assignment into a layered weather and energy analytics platform using C#, MVVM, dependency injection, SQLite, EF Core, and Blazor.
- Built a deterministic 730-day synthetic data pipeline and statistical engine for KPIs, degree days, Pearson correlation, month-over-month trends, and explainable IQR anomaly detection.
- Implemented an explainable regression forecast with time-aware train/test validation and MAE, RMSE, and R² reporting.
- Secured REST integration by replacing hardcoded credentials and manual JSON parsing with environment configuration, typed models, async `HttpClient`, timeouts, and user-safe errors.
