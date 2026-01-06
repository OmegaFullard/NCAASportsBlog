using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;
using CollegeSportsBlog.Services;
using CollegeSportsBlog.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

// Register game service (required by ScorePollingService)
builder.Services.AddSingleton<IGameService, GameService>();

// Register SignalR (required by ScorePollingService for IHubContext<ScoresHub>)
builder.Services.AddSignalR();

// Register named HttpClient for scores provider with reasonable defaults and a short timeout.
builder.Services.AddHttpClient("scores", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register the concrete scores provider - changed to Transient to avoid lifetime issues with hosted service
builder.Services.AddTransient<IScoresProvider, ScoresProvider>();

// Add hosted service for score polling
builder.Services.AddHostedService<CollegeSportsBlog.Services.ScorePollingService>();

// Rate limiting for weather endpoint: per-client IP fixed-window limiter
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy<string>("WeatherPolicy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        // Allow 30 requests per minute per IP
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static files (for index.html, CSS, JS, etc.)
app.UseDefaultFiles(); // Serves index.html as default document
app.UseStaticFiles();  // Enables serving static files from wwwroot

app.UseAuthorization();

// Apply global rate limiter middleware (endpoints opt-in by name)
app.UseRateLimiter();

// Weather proxy endpoint with caching and rate-limiting
app.MapGet("/api/weather", async (double? lat, double? lon, string? units, IMemoryCache cache, IHttpClientFactory httpFactory, HttpContext httpContext) =>
{
    // Require coordinates (client widget should provide them)
    if (lat == null || lon == null)
    {
        return Results.BadRequest(new { error = "lat and lon query parameters are required (e.g. /api/weather?lat=38.9&lon=-77.0)." });
    }

    // Validate units
    units ??= "imperial"; // default to imperial (Fahrenheit)
    units = units.ToLowerInvariant();
    if (units != "imperial" && units != "metric")
    {
        return Results.BadRequest(new { error = "units must be 'imperial' or 'metric'." });
    }

    var cacheKey = $"weather:{lat}:{lon}:{units}";
    if (cache.TryGetValue(cacheKey, out JsonElement cached))
    {
        return Results.Ok(cached);
    }

    // Build upstream request (Open-Meteo API - free, no API key required)
    var http = httpFactory.CreateClient();
    
    // Open-Meteo uses temperature_unit parameter: celsius or fahrenheit
    var temperatureUnit = units == "imperial" ? "fahrenheit" : "celsius";
    var windspeedUnit = units == "imperial" ? "mph" : "kmh";
    
    var url = $"https://api.open-meteo.com/v1/forecast?latitude={WebUtility.UrlEncode(lat.ToString())}&longitude={WebUtility.UrlEncode(lon.ToString())}&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m&temperature_unit={temperatureUnit}&wind_speed_unit={windspeedUnit}&timezone=auto";

    HttpResponseMessage upstream;
    try
    {
        upstream = await http.GetAsync(url);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: $"Upstream request failed: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
    }

    if (!upstream.IsSuccessStatusCode)
    {
        var txt = await upstream.Content.ReadAsStringAsync();
        return Results.Json(
            new { error = "Upstream weather API error", detail = txt },
            statusCode: (int)upstream.StatusCode
        );
    }

    using var stream = await upstream.Content.ReadAsStreamAsync();
    var doc = await JsonDocument.ParseAsync(stream);
    
    // Transform Open-Meteo response to match OpenWeatherMap-like structure for client compatibility
    var openMeteoData = doc.RootElement;
    var current = openMeteoData.GetProperty("current");
    
    // Map weather codes to descriptions (WMO Weather interpretation codes)
    var weatherCode = current.GetProperty("weather_code").GetInt32();
    var weatherDescription = GetWeatherDescription(weatherCode);
    
    var transformedResponse = new
    {
        coord = new { lat, lon },
        weather = new[] 
        { 
            new 
            { 
                id = weatherCode,
                main = weatherDescription.main,
                description = weatherDescription.description,
                icon = GetWeatherIcon(weatherCode)
            } 
        },
        main = new
        {
            temp = current.GetProperty("temperature_2m").GetDouble(),
            feels_like = current.GetProperty("apparent_temperature").GetDouble(),
            humidity = current.GetProperty("relative_humidity_2m").GetInt32()
        },
        wind = new
        {
            speed = current.GetProperty("wind_speed_10m").GetDouble()
        },
        name = "Location" // Open-Meteo doesn't provide city name, client can use reverse geocoding if needed
    };

    // Stronger caching: cache per-location + units for 120 seconds
    var cacheEntryOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120),
        Priority = CacheItemPriority.High
    };
    
    var responseJson = JsonSerializer.SerializeToElement(transformedResponse);
    cache.Set(cacheKey, responseJson, cacheEntryOptions);

    return Results.Ok(transformedResponse);
})
.RequireRateLimiting("WeatherPolicy");

// Helper method to convert WMO weather codes to descriptions
static (string main, string description) GetWeatherDescription(int code)
{
    return code switch
    {
        0 => ("Clear", "Clear sky"),
        1 or 2 or 3 => ("Clouds", "Partly cloudy"),
        45 or 48 => ("Fog", "Foggy"),
        51 or 53 or 55 => ("Drizzle", "Light drizzle"),
        56 or 57 => ("Drizzle", "Freezing drizzle"),
        61 or 63 or 65 => ("Rain", "Rain"),
        66 or 67 => ("Rain", "Freezing rain"),
        71 or 73 or 75 => ("Snow", "Snow"),
        77 => ("Snow", "Snow grains"),
        80 or 81 or 82 => ("Rain", "Rain showers"),
        85 or 86 => ("Snow", "Snow showers"),
        95 => ("Thunderstorm", "Thunderstorm"),
        96 or 99 => ("Thunderstorm", "Thunderstorm with hail"),
        _ => ("Unknown", "Unknown conditions")
    };
}

// Helper method to map weather codes to icon codes
static string GetWeatherIcon(int code)
{
    return code switch
    {
        0 => "01d",
        1 or 2 => "02d",
        3 => "03d",
        45 or 48 => "50d",
        51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "10d",
        71 or 73 or 75 or 77 or 85 or 86 => "13d",
        95 or 96 or 99 => "11d",
        _ => "01d"
    };
}

// Map SignalR hub for real-time score updates
app.MapHub<ScoresHub>("/hubs/scores");

// Keep existing controllers
app.MapControllers();

app.Run();
