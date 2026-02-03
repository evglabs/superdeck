using SuperDeck.Core.Models;

namespace SuperDeck.Core.Data.Repositories;

public interface IGhostRepository
{
    Task<GhostSnapshot?> GetByIdAsync(string id);
    Task<IEnumerable<GhostSnapshot>> GetByMMRRangeAsync(int minMMR, int maxMMR, int count);
    Task<GhostSnapshot> CreateAsync(GhostSnapshot ghost);
    Task UpdateStatsAsync(string ghostId, bool won, int mmrChange);
    Task<IEnumerable<GhostSnapshot>> GetAllAsync();
    Task<int> GetCountAsync();
}
