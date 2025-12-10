namespace CollegeSportsBlog.Models
{
    using System;

    /// <summary>
    /// Generic representation of an upstream game result used by the polling service.
    /// </summary>
    public sealed class ExternalGameDto
    {
        public string? ExternalId { get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public string? Status { get; set; }
        public DateTime? StartTimeUtc { get; set; }
        public DateTime? EndTimeUtc { get; set; } // New property added
    }
}