using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CollegeSportsBlog.Hubs
{
    public class ScoresHub : Hub
    {
        // Client calls to subscribe to a specific game's updates (join a SignalR group).
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game-{gameId}");
        }

        // Optional server-side helper to broadcast generic events
        public async Task BroadcastScore(object payload)
        {
            await Clients.All.SendAsync("ScoreUpdated", payload);
        }
    }
}