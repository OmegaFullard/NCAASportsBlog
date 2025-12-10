using System.Text.RegularExpressions;
using CollegeSportsBlog.Hubs;
using CollegeSportsBlog.Models;
using CollegeSportsBlog.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<ISubscriptionService, Services.SubscriptionService>();
builder.Services.AddSingleton<IGameService, Services.GameService>();
builder.Services.Configure<JsonOptions>(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddHttpClient("scores", client =>
{
    // optional default configuration for provider client
    client.Timeout = TimeSpan.FromSeconds(10);
});

// provider & background worker
builder.Services.AddSingleton<IScoresProvider, ScoresProvider>();
builder.Services.AddHostedService<ScorePollingService>();

var app = builder.Build();

app.UseCors("CorsPolicy");

// Basic endpoints for games & plays
app.MapGet("/api/games", (IGameService games) =>
{
    return Results.Ok(games.GetAll());
});

app.MapGet("/api/games/{id:guid}/plays", (Guid id, IGameService games) =>
{
    var plays = games.GetPlays(id);
    return plays is null ? Results.NotFound() : Results.Ok(plays);
});

app.MapPost("/api/games", (GameDto create, IGameService games) =>
{
    var game = games.Create(create);
    return Results.Created($"/api/games/{game.Id}", game);
});

// Update score for a game and broadcast to group
app.MapPost("/api/games/{id:guid}/score", async (Guid id, GameDto scoreUpdate, IGameService games, IHubContext<ScoresHub> hubContext) =>
{
    var updated = games.UpdateScore(id, scoreUpdate.HomeScore, scoreUpdate.AwayScore, scoreUpdate.Status);
    if (updated == null) return Results.NotFound();

    await hubContext.Clients.Group($"game-{id}").SendAsync("ScoreUpdated", updated);
    return Results.Accepted();
});

// Post a play-by-play event for a game and broadcast to group
app.MapPost("/api/games/{id:guid}/plays", async (Guid id, PlayEventDto play, IGameService games, IHubContext<ScoresHub> hubContext) =>
{
    if (!games.Exists(id)) return Results.NotFound();

    play.GameId = id;
    play.Id = Guid.NewGuid();
    play.Timestamp = play.Timestamp == default ? DateTime.UtcNow : play.Timestamp;

    games.AddPlay(id, play);

    // Broadcast to clients subscribed to the game's group
    await hubContext.Clients.Group($"game-{id}").SendAsync("PlayEvent", play);

    return Results.Created($"/api/games/{id}/plays/{play.Id}", play);
});

// Minimal subscribe endpoint (kept from earlier)
app.MapPost("/api/subscribe", async (Models.SubscribeRequest req, ISubscriptionService subscriptions) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.Email))
    {
        return Results.BadRequest(new { error = "Email is required." });
    }

    var email = req.Email.Trim();
    var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    if (!emailRegex.IsMatch(email))
    {
        return Results.BadRequest(new { error = "Invalid email address." });
    }

    var id = await subscriptions.AddAsync(email);
    return Results.Created($"/api/subscriptions/{id}", new { id, email });
});

// SignalR hub endpoint
app.MapHub<ScoresHub>("/hubs/scores");

app.Run();