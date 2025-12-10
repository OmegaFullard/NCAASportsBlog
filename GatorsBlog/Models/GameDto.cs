using System;

namespace CollegeSportsBlog.Models
{
    public sealed class GameDto
    {
        public Guid Id { get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string? Status { get; set; } // Live/Final/Quarter+clock
    }
}