# Data dictionary and SQL analysis

| Entity | Grain | Important fields |
|---|---|---|
| Location | One saved place | normalized key, city, region, country, ZIP, latitude, longitude |
| HouseholdProfile | One household configuration | floor area, occupants, heating type, default rate and location |
| WeatherObservation | One location/date/source observation | temperature, feels-like, high, low, humidity, wind, condition, provenance |
| EnergyUsageRecord | One household/location/day reading | kWh, total cost, rate, AC hours, notes, linked weather |
| ApplicationSetting | One application option | key and value |

Unique indexes prevent duplicate normalized locations, duplicate weather source observations for a location/date, and duplicate synthetic/real household readings for a date.

## Example analysis queries

```sql
-- Monthly usage and cost
SELECT strftime('%Y-%m', UsageDate) AS Month,
       ROUND(SUM(ElectricityUsageKwh), 2) AS TotalKwh,
       ROUND(SUM(TotalElectricityCost), 2) AS TotalCost
FROM EnergyUsageRecords
GROUP BY strftime('%Y-%m', UsageDate)
ORDER BY Month;

-- Temperature bands
SELECT CASE
         WHEN w.TemperatureF < 32 THEN 'Below freezing'
         WHEN w.TemperatureF < 50 THEN '32–49 F'
         WHEN w.TemperatureF < 70 THEN '50–69 F'
         WHEN w.TemperatureF < 85 THEN '70–84 F'
         ELSE '85 F and above'
       END AS TemperatureBand,
       COUNT(*) AS Days,
       ROUND(AVG(e.ElectricityUsageKwh), 2) AS AverageKwh
FROM EnergyUsageRecords e
JOIN WeatherObservations w ON w.Id = e.WeatherObservationId
GROUP BY TemperatureBand;

-- Highest usage days with weather context
SELECT e.UsageDate, l.Name, e.ElectricityUsageKwh, e.TotalElectricityCost,
       w.TemperatureF, w.HumidityPercent, w.Condition
FROM EnergyUsageRecords e
JOIN Locations l ON l.Id = e.LocationId
LEFT JOIN WeatherObservations w ON w.Id = e.WeatherObservationId
ORDER BY e.ElectricityUsageKwh DESC
LIMIT 20;
```
