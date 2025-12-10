using System;

namespace CollegeSportsBlog.Models
{
    public sealed class PlayEventDto
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public string? Team { get; set; }
        public string? Player { get; set; }
        public string? Description { get; set; } // e.g., "Touchdown — 25 yd run"
        public string? Quarter { get; set; } // e.g., "Q4"
        public string? Clock { get; set; } // e.g., "02:13"
        public DateTime Timestamp { get; set; }
    }
}