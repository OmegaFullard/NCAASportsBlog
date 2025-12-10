using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CollegeSportsBlog.Models;

namespace CollegeSportsBlog.Services
{
    // Simple in-memory game & plays store. Replace with DB in production.
    public class GameService : IGameService
    {
        private readonly ConcurrentDictionary<Guid, GameDto> _games = new();
        private readonly ConcurrentDictionary<Guid, List<PlayEventDto>> _plays = new();

        public GameService()
        {
            // seed example game
            var g = new GameDto
            {
                Id = Guid.NewGuid(),
                HomeTeam = "College A",
                AwayTeam = "College B",
                HomeScore = 0,
                AwayScore = 0,
                Status = "Scheduled"
            };
            _games.TryAdd(g.Id, g);
            _plays.TryAdd(g.Id, new List<PlayEventDto>());
        }

        public IEnumerable<GameDto> GetAll() => _games.Values.OrderBy(g => g.Status).ToArray();

        public GameDto Create(GameDto game)
        {
            game.Id = Guid.NewGuid();
            _games.TryAdd(game.Id, game);
            _plays.TryAdd(game.Id, new List<PlayEventDto>());
            return game;
        }

        public bool Exists(Guid id) => _games.ContainsKey(id);

        public GameDto? UpdateScore(Guid id, int home, int away, string? status)
        {
            if (_games.TryGetValue(id, out var g))
            {
                g.HomeScore = home;
                g.AwayScore = away;
                g.Status = status;
                return g;
            }
            return null;
        }

        public void AddPlay(Guid gameId, PlayEventDto play)
        {
            if (!_plays.TryGetValue(gameId, out var list))
            {
                list = new List<PlayEventDto>();
                _plays.TryAdd(gameId, list);
            }
            list.Insert(0, play); // newest first
        }

        public IEnumerable<PlayEventDto>? GetPlays(Guid gameId)
        {
            return _plays.TryGetValue(gameId, out var list) ? list.AsReadOnly() : null;
        }
    }
}