using SuperDeck.Core.Models;

namespace SuperDeck.Core.Data.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(string id);
    Task<IEnumerable<Character>> GetByPlayerIdAsync(string? playerId);
    Task<IEnumerable<Character>> GetAllAsync();
    Task<Character> CreateAsync(Character character);
    Task<Character> UpdateAsync(Character character);
    Task<bool> DeleteAsync(string id);
    Task<IEnumerable<Character>> GetGhostsByMMRRangeAsync(int minMMR, int maxMMR, int count);
    Task UpdateStatsAsync(string characterId, bool won);
}
