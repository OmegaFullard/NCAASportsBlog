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
    // Background worker polling the scores provider and updating local GameService + broadcasting.
    public class ScorePollingService : BackgroundService
    {
        private readonly IScoresProvider _provider;
        private readonly IGameService _games;
        private readonly IHubContext<ScoresHub> _hub;
        private readonly ILogger<ScorePollingService> _log;
        private readonly TimeSpan _interval;

        public ScorePollingService(IScoresProvider provider, IGameService games, IHubContext<ScoresHub> hub, IConfiguration config, ILogger<ScorePollingService> log)
        {
            _provider = provider;
            _games = games;
            _hub = hub;
            _log = log;

            var seconds = config.GetValue<int?>("SCORES_POLL_INTERVAL_SECONDS") ?? 15;
            _interval = TimeSpan.FromSeconds(Math.Max(5, seconds));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("ScorePollingService started, interval {Interval}s", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var externalGames = await _provider.GetLiveGamesAsync();

                    // Map upstream games to local games and update
                    var localGames = _games.GetAll().ToArray();

                    foreach (var eg in externalGames)
                    {
                        // Best-effort matching: match by team names (case-insensitive). Improve with upstream ids if available.
                        var match = localGames.FirstOrDefault(g =>
                            string.Equals(g.HomeTeam?.Trim(), eg.HomeTeam?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(g.AwayTeam?.Trim(), eg.AwayTeam?.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (match != null)
                        {
                            var home = eg.HomeScore ?? match.HomeScore;
                            var away = eg.AwayScore ?? match.AwayScore;
                            var status = eg.Status ?? match.Status;

                            var updated = _games.UpdateScore(match.Id, home, away, status);
                            if (updated != null)
                            {
                                // Broadcast to the game's group
                                await _hub.Clients.Group($"game-{match.Id}").SendAsync("ScoreUpdated", updated, cancellationToken: stoppingToken);
                            }
                        }
                        else
                        {
                            // Optionally create/upsert local game if not found.
                            _log.LogDebug("No local match found for upstream game {Home} vs {Away}", eg.HomeTeam, eg.AwayTeam);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error polling scores provider.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}