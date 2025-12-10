using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CollegeSportsBlog.Hubs
{
    public class ScoresHub : Hub
    {
        // Called by server/admin to broadcast updates (server-side method used via IHubContext)
        // Clients listen for "ScoreUpdated" messages.
        public async Task BroadcastScoreAsync(object payload)
        {
            await Clients.All.SendAsync("ScoreUpdated", payload);
        }
    }
}