using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using CollegeSportsBlog.Hubs;
using CollegeSportsBlog.Models;

namespace CollegeSportsBlog.Services
{
    /// <summary>
    /// Background service that polls the configured <see cref="IScoresProvider"/>,

    /// updates the local <see cref="IGameService"/> and broadcasts changes via SignalR.
    /// </summary>
    public sealed class ScorePollingService : BackgroundService
    {
        private readonly IScoresProvider _provider;
        private readonly IGameService _games;
        private readonly IHubContext<ScoresHub> _hub;
        private readonly ILogger<ScorePollingService> _log;
        private readonly TimeSpan _interval;

        public ScorePollingService(
            IScoresProvider provider,
            IGameService games,
            IHubContext<ScoresHub> hub,
            IConfiguration config,
            ILogger<ScorePollingService> log)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _games = games ?? throw new ArgumentNullException(nameof(games));
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            var seconds = config.GetValue<int?>("SCORES_POLL_INTERVAL_SECONDS") ?? 15;
            _interval = TimeSpan.FromSeconds(Math.Max(5, seconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("ScorePollingService starting. Poll interval: {Interval}s", _interval.TotalSeconds);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var externalGames = (await _provider.GetLiveGamesAsync()).ToArray();
                        if (externalGames.Length == 0)
                        {
                            _log.LogDebug("No games returned by scores provider.");
                        }
                        else
                        {
                            var localGames = _games.GetAll().ToArray();

                            foreach (var eg in externalGames)
                            {
                                if (eg == null) continue;

                                // Normalize team names for best-effort matching
                                string Normalize(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

                                var match = localGames.FirstOrDefault(local =>
                                    !string.IsNullOrWhiteSpace(local.HomeTeam) &&
                                    !string.IsNullOrWhiteSpace(local.AwayTeam) &&
                                    Normalize(local.HomeTeam) == Normalize(eg.HomeTeam) &&
                                    Normalize(local.AwayTeam) == Normalize(eg.AwayTeam));

                                if (match != null)
                                {
                                    var newHome = eg.HomeScore ?? match.HomeScore;
                                    var newAway = eg.AwayScore ?? match.AwayScore;
                                    var newStatus = eg.Status ?? match.Status;

                                    // Only update when something changed
                                    if (newHome != match.HomeScore || newAway != match.AwayScore || newStatus != match.Status)
                                    {
                                        var updated = _games.UpdateScore(match.Id, newHome, newAway, newStatus);
                                        if (updated != null)
                                        {
                                            _log.LogInformation("Updated score for {Home} vs {Away}: {H}-{A} ({Status})",
                                                updated.HomeTeam, updated.AwayTeam, updated.HomeScore, updated.AwayScore, updated.Status);

                                            // Broadcast to clients in the game's group
                                            await _hub.Clients.Group($"game-{match.Id}")
                                                .SendAsync("ScoreUpdated", updated, stoppingToken);
                                        }
                                    }
                                }
                                else
                                {
                                    // Optionally create a new local game for upstream match
                                    if (!string.IsNullOrWhiteSpace(eg.HomeTeam) && !string.IsNullOrWhiteSpace(eg.AwayTeam))
                                    {
                                        var created = _games.Create(new GameDto
                                        {
                                            Id = Guid.NewGuid(),
                                            HomeTeam = eg.HomeTeam,
                                            AwayTeam = eg.AwayTeam,
                                            HomeScore = eg.HomeScore ?? 0,
                                            AwayScore = eg.AwayScore ?? 0,
                                            Status = eg.Status ?? "Live"
                                        });

                                        _log.LogInformation("Created local game for upstream match {Home} vs {Away} (id: {Id})",
                                            created.HomeTeam, created.AwayTeam, created.Id);

                                        // Broadcast initial score to group (clients can join after creation)
                                        await _hub.Clients.Group($"game-{created.Id}")
                                            .SendAsync("ScoreUpdated", created, stoppingToken);
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // shutdown requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Error while polling scores provider.");
                    }

                    await Task.Delay(_interval, stoppingToken);
                }
            }
            finally
            {
                _log.LogInformation("ScorePollingService stopping.");
            }
        }
    }
}