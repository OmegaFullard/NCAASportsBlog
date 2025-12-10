Models/ScoreUpdateDto.cs
namespace CollegeSportsBlog.Models
{
    public sealed class ScoreUpdateDto
    {
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public string? Status { get; set; } // e.g., "Final", "Q4 02:13", "Halftime"
    }
}