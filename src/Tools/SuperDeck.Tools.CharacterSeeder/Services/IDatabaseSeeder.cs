using SuperDeck.Core.Models;

namespace SuperDeck.Tools.CharacterSeeder.Services;

public interface IDatabaseSeeder
{
    Task UpsertCharacterAsync(Character character);
}
