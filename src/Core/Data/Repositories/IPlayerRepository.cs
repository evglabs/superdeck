using SuperDeck.Core.Models;

namespace SuperDeck.Core.Data.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(string id);
    Task<Player?> GetByUsernameAsync(string username);
    Task<Player> CreateAsync(Player player);
    Task<Player?> UpdateAsync(Player player);
    Task<bool> UsernameExistsAsync(string username);
}
