using SuperDeck.Core.Models;

namespace SuperDeck.Core.Data.Repositories;

public interface IAIProfileRepository
{
    Task<AIProfile?> GetByIdAsync(string id);
    Task<IEnumerable<AIProfile>> GetAllAsync();
    Task<AIProfile> CreateAsync(AIProfile profile);
    Task<AIProfile?> GetDefaultAsync();
}
