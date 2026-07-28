using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using WattWeather.Server.Endpoints;
using WattWeather.Server.Security;
using WattWeather.Server.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576;
    options.AddServerHeader = false;
});

builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("locations", policy => policy.Expire(TimeSpan.FromHours(1)).SetVaryByQuery("query"));
    options.AddPolicy("weather", policy => policy.Expire(TimeSpan.FromMinutes(10)).SetVaryByQuery("latitude", "longitude"));
    options.AddPolicy("states", policy => policy.Expire(TimeSpan.FromHours(6)));
    options.AddPolicy("correlation", policy => policy.Expire(TimeSpan.FromHours(12)).SetVaryByQuery("latitude", "longitude", "state"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/429",
            title = "Too many requests",
            status = 429,
            detail = "Please wait a moment before requesting more city data."
        }, cancellationToken);
    };
    options.AddPolicy("api", context =>
    {
        var client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
builder.Services.AddSingleton<StateEnergyRepository>();
builder.Services.AddHttpClient<OpenMeteoClient>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(12);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("WattWeather/2.0 (+https://github.com/sampbaer-creator/WattWeather)");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1,
    KnownProxies = { IPAddress.Loopback, IPAddress.IPv6Loopback }
});
app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseResponseCompression();
app.UseRateLimiter();
app.UseOutputCache();
app.UseBlazorFrameworkFiles();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var path = context.Context.Request.Path.Value ?? "";
        context.Context.Response.Headers.CacheControl =
            path is "/" or "/index.html"
                ? "no-cache"
                : path.Contains("_framework", StringComparison.Ordinal)
                    ? "public,max-age=2592000,immutable"
                    : "public,max-age=3600";
    }
});

app.MapWattWeatherApi();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }))
    .ExcludeFromDescription();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
