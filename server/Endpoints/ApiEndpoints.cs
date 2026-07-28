using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using WattWeather.Server.Services;

namespace WattWeather.Server.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapWattWeatherApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api")
            .RequireRateLimiting("api");

        api.MapGet("/locations", async (
            [FromQuery] string query,
            OpenMeteoClient weather,
            CancellationToken cancellationToken) =>
        {
            query = query.Trim();
            return query.Length is < 2 or > 80
                ? Results.BadRequest(new { error = "Query must contain between 2 and 80 characters." })
                : Results.Ok(await weather.SearchCitiesAsync(query, cancellationToken));
        }).CacheOutput("locations");

        api.MapGet("/weather", async (
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            OpenMeteoClient weather,
            CancellationToken cancellationToken) =>
        {
            if (!ValidCoordinates(latitude, longitude))
            {
                return Results.BadRequest(new { error = "Coordinates are outside valid ranges." });
            }

            return Results.Ok(await weather.GetForecastAsync(latitude, longitude, cancellationToken));
        }).CacheOutput("weather");

        api.MapGet("/states", async (StateEnergyRepository repository, CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetDatasetAsync(cancellationToken)))
            .CacheOutput("states");

        api.MapGet("/correlation", async (
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] string state,
            OpenMeteoClient weather,
            CancellationToken cancellationToken) =>
        {
            state = state.Trim();
            if (!ValidCoordinates(latitude, longitude) || state.Length is < 2 or > 60)
            {
                return Results.BadRequest(new { error = "Coordinates or state are invalid." });
            }

            return Results.Ok(await weather.GetCorrelationAsync(latitude, longitude, state, cancellationToken));
        }).CacheOutput("correlation");

        return endpoints;
    }

    private static bool ValidCoordinates(double latitude, double longitude) =>
        double.IsFinite(latitude) && double.IsFinite(longitude) &&
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180;
}
