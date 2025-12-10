namespace CollegeSportsBlog.Services
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CollegeSportsBlog.Models;

    /// <summary>
    /// Abstraction for fetching live scores from an upstream provider.
    /// </summary>
    public interface IScoresProvider
    {
        /// <summary>
        /// Returns a list of live/current games (provider-agnostic DTO).
        /// </summary>
        Task<IEnumerable<ExternalGameDto>> GetLiveGamesAsync();
    }
}