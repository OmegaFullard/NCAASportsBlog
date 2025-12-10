using System;
using System.Collections.Generic;
using CollegeSportsBlog.Models;

namespace CollegeSportsBlog.Services
{
    public interface IGameService
    {
        IEnumerable<GameDto> GetAll();
        GameDto Create(GameDto game);
        bool Exists(Guid id);
        GameDto? UpdateScore(Guid id, int home, int away, string? status);
        void AddPlay(Guid gameId, PlayEventDto play);
        IEnumerable<PlayEventDto>? GetPlays(Guid gameId);
    }
}