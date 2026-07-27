# Power BI and Azure expansion

## Recommended star schema

- `FactEnergyDaily`: date, household, location, weather, kWh, cost, rate, anomaly score, predicted kWh
- `DimDate`: calendar, month, quarter, season, weekend, holiday
- `DimLocation`: city, region, country, climate zone
- `DimHousehold`: size band, occupants, heating type
- `DimWeather`: condition and temperature band

Suggested measures: Total kWh, Total Cost, Average Daily kWh, Average Rate, Month-over-Month %, Weather-Normalized Usage, Forecast Error, Anomaly Count, HDD, and CDD.

Suggested report pages: Executive Overview, Weather Drivers, Cost & Usage Trends, Household Comparison, Forecast Accuracy, and Data Quality.

## Azure SQL migration

1. Introduce EF Core migrations and replace the SQLite provider with `Microsoft.EntityFrameworkCore.SqlServer`.
2. Store the connection string in Azure Key Vault or App Service configuration.
3. Review decimal precision, date/time semantics, retry policy, indexes, and migration permissions.
4. Add managed identity and least-privilege database roles.
5. Use an ETL job or read-only reporting replica for Power BI.

Power BI can connect using the Azure SQL connector in Import or DirectQuery mode. Use a gateway only when the database remains private/on-premises. Azure and Power BI are optional; the local app continues to use SQLite.
