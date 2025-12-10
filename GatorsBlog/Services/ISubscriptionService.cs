using System;
using System.Threading.Tasks;

namespace CollegeSportsBlog.Services
{
    public interface ISubscriptionService
    {
        Task<Guid> AddAsync(string email);
        Task<bool> ExistsAsync(string email);
    }
}