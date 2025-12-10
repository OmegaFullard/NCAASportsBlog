using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CollegeSportsBlog.Services
{
    // Simple in-memory subscription store; replace with DB or external provider.
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ConcurrentDictionary<Guid, string> _store = new();

        public Task<Guid> AddAsync(string email)
        {
            var id = Guid.NewGuid();
            _store.TryAdd(id, email);
            return Task.FromResult(id);
        }

        public Task<bool> ExistsAsync(string email)
        {
            var exists = _store.Values.Contains(email, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(exists);
        }
    }
}