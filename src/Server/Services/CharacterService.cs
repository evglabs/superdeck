using System.Text.Json;
using SuperDeck.Core.Data.Repositories;
using SuperDeck.Core.Models;
using SuperDeck.Core.Models.Enums;
using SuperDeck.Core.Settings;

namespace SuperDeck.Server.Services;

public class CharacterService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IGhostRepository _ghostRepository;
    private readonly CardService _cardService;
    private readonly GameSettings _settings;

    public CharacterService(
        ICharacterRepository characterRepository,
        IGhostRepository ghostRepository,
        CardService cardService,
        GameSettings settings)
    {
        _characterRepository = characterRepository;
        _ghostRepository = ghostRepository;
        _cardService = cardService;
        _settings = settings;
    }

    public async Task<Character> CreateCharacterAsync(string name, Suit suitChoice, string? playerId = null)
    {
        var character = new Character
        {
            Name = name,
            Level = 1,
            Attack = _settings.Character.StartingAttack,
            Defense = _settings.Character.StartingDefense,
            Speed = _settings.Character.StartingSpeed,
            OwnerPlayerId = playerId
        };

        // Starter deck is just Punch + Block cards
        var basicDeck = _cardService.GetBasicStarterDeck();
        character.DeckCardIds = basicDeck.Select(c => c.Id).ToList();

        return await _characterRepository.CreateAsync(character);
    }

    public async Task<Character?> AddCardsToDeckAsync(string characterId, List<string> cardIds)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null) return null;

        character.DeckCardIds.AddRange(cardIds);
        character.LastModified = DateTime.UtcNow;

        return await _characterRepository.UpdateAsync(character);
    }

    public async Task<Character?> RemoveCardsFromDeckAsync(string characterId, List<string> cardIds)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null) return null;

        foreach (var cardId in cardIds)
        {
            // Remove only one instance of each card ID (for duplicates)
            var index = character.DeckCardIds.IndexOf(cardId);
            if (index >= 0)
            {
                character.DeckCardIds.RemoveAt(index);
            }
        }

        character.LastModified = DateTime.UtcNow;
        return await _characterRepository.UpdateAsync(character);
    }

    public async Task<Character?> GetCharacterAsync(string id)
    {
        return await _characterRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Character>> GetCharactersAsync(string? playerId = null)
    {
        return await _characterRepository.GetByPlayerIdAsync(playerId);
    }

    public async Task<Character?> AllocateStatsAsync(string characterId, int attack, int defense, int speed, int bonusHP = 0)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null) return null;

        if (attack < 0 || defense < 0 || speed < _settings.Character.MinSpeed || bonusHP < 0)
        {
            throw new InvalidOperationException($"Stats cannot be negative, and speed must be at least {_settings.Character.MinSpeed}");
        }

        if (attack % _settings.Character.AttackPerStatPoint != 0)
        {
            throw new InvalidOperationException($"Attack must be a multiple of {_settings.Character.AttackPerStatPoint}");
        }

        if (defense % _settings.Character.DefensePerStatPoint != 0)
        {
            throw new InvalidOperationException($"Defense must be a multiple of {_settings.Character.DefensePerStatPoint}");
        }

        if (speed % _settings.Character.SpeedPerStatPoint != 0)
        {
            throw new InvalidOperationException($"Speed must be a multiple of {_settings.Character.SpeedPerStatPoint}");
        }

        if (bonusHP % _settings.Character.HPPerStatPoint != 0)
        {
            throw new InvalidOperationException($"BonusHP must be a multiple of {_settings.Character.HPPerStatPoint}");
        }

        int attackPoints = attack / _settings.Character.AttackPerStatPoint;
        int defensePoints = defense / _settings.Character.DefensePerStatPoint;
        int speedPoints = speed / _settings.Character.SpeedPerStatPoint;
        int hpPoints = bonusHP / _settings.Character.HPPerStatPoint;

        // Validate stat allocation
        int totalStats = attackPoints + defensePoints + speedPoints + hpPoints;
        int allowedStats = character.Level * _settings.Character.StatPointsPerLevel;

        if (totalStats > allowedStats)
        {
            throw new InvalidOperationException($"Invalid stat allocation. Total: {totalStats}, Allowed: {allowedStats}");
        }

        character.Attack = attack;
        character.Defense = defense;
        character.Speed = speed;
        character.BonusHP = bonusHP;
        character.LastModified = DateTime.UtcNow;

        return await _characterRepository.UpdateAsync(character);
    }

    public async Task<bool> DeleteCharacterAsync(string id)
    {
        return await _characterRepository.DeleteAsync(id);
    }

    public async Task<Character?> AddXPAsync(string characterId, int xp)
    {
        var character = await _characterRepository.GetByIdAsync(characterId);
        if (character == null) return null;

        character.XP += xp;

        // Check for level up
        while (character.Level < _settings.Character.MaxLevel && character.XP >= GetXPForNextLevel(character.Level))
        {
            character.XP -= GetXPForNextLevel(character.Level);
            character.Level++;

            // Create ghost snapshot on level up
            await CreateGhostSnapshotAsync(character);
        }

        character.LastModified = DateTime.UtcNow;
        return await _characterRepository.UpdateAsync(character);
    }

    public int GetXPForNextLevel(int currentLevel)
    {
        return _settings.XP.BaseXPForLevelUp + ((currentLevel - 1) * _settings.XP.XPIncreasePerLevel);
    }

    public int GetAvailableStatPoints(Character character)
    {
        int totalAllowed = character.Level * _settings.Character.StatPointsPerLevel;
        int attackPoints = character.Attack / _settings.Character.AttackPerStatPoint;
        int defensePoints = character.Defense / _settings.Character.DefensePerStatPoint;
        int speedPoints = character.Speed / _settings.Character.SpeedPerStatPoint;
        int hpPoints = character.BonusHP / _settings.Character.HPPerStatPoint;
        int totalUsed = attackPoints + defensePoints + speedPoints + hpPoints;
        return totalAllowed - totalUsed;
    }

    private async Task CreateGhostSnapshotAsync(Character character)
    {
        var ghost = new GhostSnapshot
        {
            SourceCharacterId = character.Id,
            SerializedCharacterState = JsonSerializer.Serialize(character),
            GhostMMR = character.MMR,
            AIProfileId = "default"
        };

        await _ghostRepository.CreateAsync(ghost);
    }
}
